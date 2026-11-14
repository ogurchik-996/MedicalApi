-- Задание 5. SQL, который EF Core отправил в PostgreSQL (скопировано из консоли dotnet run).
-- Вместо значений параметров в логе стоит '?': EF Core не пишет в лог персональные данные пациентов.
-- Чтобы видеть значения на своём компьютере, можно временно дописать .EnableSensitiveDataLogging() после UseNpgsql(...)

-- ============================================================
-- GET /api/patients
-- ============================================================
SELECT p."Id", p."Address", p."BirthDate", p."HasInsurance", p."Iin", p."IsMale", p."Name", p."Phone", p."Uchastok"
FROM "Patients" AS p
ORDER BY p."Name";

-- ============================================================
-- GET /api/patients/1      [Parameters=[@__id_0='?' (DbType = Int32)]]
-- ============================================================
SELECT p."Id", p."Address", p."BirthDate", p."HasInsurance", p."Iin", p."IsMale", p."Name", p."Phone", p."Uchastok"
FROM "Patients" AS p
WHERE p."Id" = @__id_0
LIMIT 1;

-- Чем отличается от GET /api/patients?
-- GET всех: без WHERE, вся таблица, ORDER BY по имени (у нас в GetAll стоит OrderBy).
-- GET по id: WHERE "Id" = @__id_0 и LIMIT 1, потому что FirstOrDefaultAsync нужна одна строка.
-- id передаётся параметром @__id_0, а не вписывается в текст SQL: так база не путает данные с командой (защита от SQL-инъекций)

-- ============================================================
-- POST /api/patients
-- ============================================================
-- 1) проверка из Validate: нет ли уже пациента с таким ИИН (AnyAsync -> EXISTS)
SELECT EXISTS (
    SELECT 1
    FROM "Patients" AS p
    WHERE p."Iin" = @__patient_Iin_0 AND p."Id" <> @__id_1);

-- 2) SaveChangesAsync -> INSERT
INSERT INTO "Patients" ("Address", "BirthDate", "HasInsurance", "Iin", "IsMale", "Name", "Phone", "Uchastok")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)
RETURNING "Id";

-- Как приложение узнаёт id, который выдала база?
-- "Id" нет в списке столбцов INSERT, его выдаёт база (identity). RETURNING "Id" просит PostgreSQL
-- вернуть выданный номер в ответе на INSERT, и EF Core записывает его в patient.Id.
-- Поэтому после SaveChangesAsync() patient.Id уже заполнен и попадает в ответ 201

-- ============================================================
-- PUT /api/patients/1, изменён только телефон
-- ============================================================
-- 1) найти пациента
SELECT p."Id", p."Address", p."BirthDate", p."HasInsurance", p."Iin", p."IsMale", p."Name", p."Phone", p."Uchastok"
FROM "Patients" AS p
WHERE p."Id" = @__id_0
LIMIT 1;

-- 2) проверка ИИН
SELECT EXISTS (
    SELECT 1
    FROM "Patients" AS p
    WHERE p."Iin" = @__patient_Iin_0 AND p."Id" <> @__id_1);

-- 3) SaveChangesAsync -> UPDATE
UPDATE "Patients" SET "Phone" = @p0
WHERE "Id" = @p1;

-- Шаг 4. Какие столбцы в UPDATE? Только "Phone", хотя в контроллере мы присвоили все 8 полей.
-- EF Core запомнил значения, которые прочитал из базы (change tracking), и при SaveChangesAsync
-- сравнил их с текущими. В UPDATE попадают только реально изменённые столбцы.
--
-- PUT с теми же данными, что уже лежат в базе: были только SELECT и SELECT EXISTS, UPDATE не было вообще.
-- Ничего не изменилось, EF Core нечего сохранять, и в базу запрос не уходит

-- ============================================================
-- DELETE /api/patients/2
-- ============================================================
-- 1) найти пациента
SELECT p."Id", p."Address", p."BirthDate", p."HasInsurance", p."Iin", p."IsMale", p."Name", p."Phone", p."Uchastok"
FROM "Patients" AS p
WHERE p."Id" = @__id_0
LIMIT 1;

-- 2) SaveChangesAsync -> DELETE
DELETE FROM "Patients"
WHERE "Id" = @p0;

-- После задания 8 перед DELETE ещё проверка, есть ли у пациента справки (CountAsync):
-- SELECT count(*)::int FROM "Spravki" AS s WHERE s."PatientId" = @__id_0

-- ============================================================
-- Шаг 3. Одна база, два клиента
-- ============================================================
-- Пациент, созданный в Swagger, находится в pgAdmin:
SELECT "Id", "Name", "BirthDate", "Uchastok" FROM "Patients" WHERE "Id" = 1;
-- Меняем поле в pgAdmin:
UPDATE "Patients" SET "Uchastok" = 7 WHERE "Id" = 1;
-- GET /api/patients/1 в Swagger сразу показывает "uchastok": 7.
-- Приложение не хранит данные у себя: при каждом запросе оно спрашивает базу

-- ============================================================
-- Задание 6. LINQ: где выполняется запрос
-- ============================================================

-- Вариант A: _db.Spravki.Where(s => s.Price > 300).ToListAsync()
SELECT s."Id", s."IssueDate", s."Name", s."PatientId", s."Price", s."SpravkaTypeId", s."ValidUntil"
FROM "Spravki" AS s
WHERE s."Price" > 300.0;

-- Вариант B: _db.Spravki.ToListAsync(), потом .Where(...) в C#
SELECT s."Id", s."IssueDate", s."Name", s."PatientId", s."Price", s."SpravkaTypeId", s."ValidUntil"
FROM "Spravki" AS s;

-- Результат одинаковый (2 справки), но WHERE есть только в A. В B база отдаёт всю таблицу,
-- и фильтрует уже C#. При миллионе справок B перекачает миллион строк по сети и создаст миллион
-- объектов в памяти сервера, чтобы потом выбросить почти все. A отдаст только нужные строки,
-- а база может использовать индекс.

-- GET /api/patients (без name): SQL как выше, без WHERE.
-- GET /api/patients?name=ай: появляется WHERE с ILIKE (без учёта регистра, находит «Айбек»).
-- С Contains было бы LIKE с учётом регистра, и «ай» не нашло бы «Айбек»
SELECT p."Id", p."Address", p."BirthDate", p."HasInsurance", p."Iin", p."IsMale", p."Name", p."Phone", p."Uchastok"
FROM "Patients" AS p
WHERE p."Name" ILIKE @__Format_1 ESCAPE ''
ORDER BY p."Name";

-- GET /api/patients/1/spravki: проверка, что пациент есть, потом справки через WHERE в базе
SELECT EXISTS (
    SELECT 1
    FROM "Patients" AS p
    WHERE p."Id" = @__id_0);
SELECT s."Id", s."IssueDate", s."Name", s."PatientId", s."Price", s."SpravkaTypeId", s."ValidUntil"
FROM "Spravki" AS s
WHERE s."PatientId" = @__id_0
ORDER BY s."IssueDate";

-- GET /api/stats: 4 отдельных SQL-запроса (по одному на каждый CountAsync/SumAsync).
-- COUNT и SUM считает база, в C# приходят только числа
SELECT count(*)::int FROM "Patients" AS p;
SELECT count(*)::int FROM "Spravki" AS s;
SELECT COALESCE(sum(s."Price"), 0.0) FROM "Spravki" AS s;
SELECT count(*)::int FROM "Appointments" AS a;

-- Бонус: GET /api/spravki?patientId=1&minPrice=200&sort=price_desc, все фильтры в одном WHERE
SELECT s."Id", s."IssueDate", s."Name", s."PatientId", s."Price", s."SpravkaTypeId", s."ValidUntil"
FROM "Spravki" AS s
WHERE s."PatientId" = @__patientId_0 AND s."Price" >= @__minPrice_1
ORDER BY s."Price" DESC, s."Id";

-- ============================================================
-- Задание 7. Миграции
-- ============================================================
-- Добавили Email в Patient без миграции: GET /api/patients -> 500,
-- Npgsql.PostgresException: 42703: столбец p.Email не существует.
-- EF Core уже просит p."Email", а в таблице такого столбца нет.
--
-- Миграция AddPatientEmail:
--   Up   -> ALTER TABLE "Patients" ADD "Email" text;   (применить изменение)
--   Down -> ALTER TABLE "Patients" DROP COLUMN "Email"; (откатить изменение)
-- У старых пациентов в Email лежит NULL. В __EFMigrationsHistory две строки: ..._Init и ..._AddPatientEmail
SELECT "Id", "Email", "Email" IS NULL AS is_null FROM "Patients";
SELECT * FROM "__EFMigrationsHistory";

-- ============================================================
-- Задание 8. Удаление пациента со справками
-- ============================================================
-- С миграцией Init (onDelete: ReferentialAction.Cascade): пациенту 5 выписали 2 справки,
-- DELETE /api/patients/5 -> 204, справки 5 и 6 пропали молча: база удалила их каскадом.
--
-- Решение: справки это медицинские документы, их нельзя терять. Миграция RestrictDeleteSpravki:
--   FK_Spravki_Patients_PatientId          ON DELETE RESTRICT
--   FK_Spravki_SpravkaTypes_SpravkaTypeId  ON DELETE RESTRICT (вид справки тоже нельзя удалить, если по нему есть справки)
-- В контроллере перед удалением CountAsync по справкам, и API отвечает 400:
--   «Нельзя удалить пациента Айбек Асанов: у него выписано справок: 2. Справки это медицинские документы»
-- Даже в обход API база не даст удалить:
DELETE FROM "Patients" WHERE "Id" = 1;
-- ОШИБКА: UPDATE или DELETE в таблице "Patients" нарушает ограничение RESTRICT внешнего ключа
--         "FK_Spravki_Patients_PatientId" таблицы "Spravki"
-- Записи на приём остаются Cascade: это расписание, без пациента они не нужны.
-- Удаление справки: запись на приём остаётся, ссылка SpravkaId становится NULL (ON DELETE SET NULL)

-- ============================================================
-- Задание 9. Баги
-- ============================================================
-- Чужой номер: если в POST прислать "id": 1, EF Core отправляет INSERT с "Id" = 1, и база отвечает
-- duplicate key value violates unique constraint "PK_Patients" (500).
-- Если прислать "id": 50, вставка пройдёт, но счётчик identity об этом не знает. Когда база сама дойдёт
-- до 50, INSERT упадёт с тем же duplicate key, и пациента будет не создать.
-- Исправление: во всех Create (пациенты, справки, виды справок, записи на приём) перед Add стоит Id = 0,
-- номер всегда выдаёт база. Проверено: POST с "id": 1 -> 201 и id 6, с "id": 50 -> 201 и id 7.
--
-- Забыли сохранить: без await _db.SaveChangesAsync() POST вернул 201 и "id": 0.
-- INSERT в консоли не было (только SELECT EXISTS из проверки ИИН), в GET и в pgAdmin пациента нет.
-- Add только запоминает объект в контексте, а в базу ничего не уходит до SaveChangesAsync.
-- Контекст живёт один HTTP-запрос, после ответа несохранённый пациент просто пропал
