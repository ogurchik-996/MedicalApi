-- Задание 2. База клиники на SQL (выполнять в базе clinic)
-- База создана командой: CREATE DATABASE clinic;

-- ============================================================
-- Шаг 2. Таблицы
-- ============================================================

-- пациенты: номер выдаёт база, ФИО и дата рождения обязательны, телефон можно не указывать
CREATE TABLE patients (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    birth_date DATE NOT NULL,
    phone VARCHAR(20)
);

-- каталог видов справок: текущая цена, база не даст сохранить цену <= 0
CREATE TABLE spravka_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    price NUMERIC(10, 2) NOT NULL CHECK (price > 0),
    valid_days INT NOT NULL
);

-- выданные справки: ссылаются на пациента и вид справки, хранят цену продажи; создаётся последней
CREATE TABLE spravki (
    id SERIAL PRIMARY KEY,
    patient_id INT NOT NULL REFERENCES patients(id),
    spravka_type_id INT NOT NULL REFERENCES spravka_types(id),
    price NUMERIC(10, 2) NOT NULL,
    issued_at DATE NOT NULL DEFAULT CURRENT_DATE
);

-- ============================================================
-- Шаг 3. Данные
-- ============================================================

-- пациенты из задания (id 1-5)
INSERT INTO patients (name, birth_date, phone) VALUES
    ('Айбек Асанов', '1998-03-12', '0555123456'),
    ('Нургуль Токтосунова', '2003-07-25', '0700987654'),
    ('Бакыт Жумабаев', '1985-11-02', NULL),
    ('Айпери Садыкова', '2001-01-30', '0772345678'),
    ('Тимур Ким', '2006-09-14', '0509111222');

-- каталог видов справок из задания (id 1-4)
INSERT INTO spravka_types (name, price, valid_days) VALUES
    ('Справка 083', 100, 365),
    ('Санитарная книжка', 750, 365),
    ('Справка 086', 300, 180),
    ('Справка в бассейн', 200, 90);

-- справки из задания (id 1-7)
INSERT INTO spravki (patient_id, spravka_type_id, price, issued_at) VALUES
    (1, 1, 100, '2026-10-01'),
    (1, 4, 200, '2026-10-15'),
    (2, 2, 750, '2026-10-03'),
    (2, 3, 300, '2026-11-02'),
    (4, 3, 300, '2026-11-05'),
    (5, 4, 200, '2026-11-06'),
    (5, 1, 100, '2026-11-10');

-- свои пациенты (id 6-8), у одного нет телефона
INSERT INTO patients (name, birth_date, phone) VALUES
    ('Жылдыз Найманова', '1999-05-18', '0555777888'),
    ('Эрлан Мамытов', '2010-12-01', NULL),
    ('айжан Ормонова', '1976-02-09', '0701234567');

-- свой вид справки (id 5)
INSERT INTO spravka_types (name, price, valid_days) VALUES
    ('Справка в спортивную секцию', 250, 365);

-- свои справки (id 8-12)
INSERT INTO spravki (patient_id, spravka_type_id, price, issued_at) VALUES
    (6, 2, 750, '2026-09-20'),
    (7, 5, 250, '2026-11-03'),
    (7, 4, 200, '2026-11-08'),
    (8, 1, 100, '2026-10-28'),
    (3, 3, 300, '2026-11-12');

-- ============================================================
-- Шаг 4. JOIN трёх таблиц и статистика
-- ============================================================

-- все справки с именем пациента и названием справки, по дате выдачи
SELECT spravki.id, patients.name AS patient, spravka_types.name AS spravka, spravki.price, spravki.issued_at
FROM spravki
JOIN patients ON spravki.patient_id = patients.id
JOIN spravka_types ON spravki.spravka_type_id = spravka_types.id
ORDER BY spravki.issued_at;

-- сколько всего справок и на какую сумму
SELECT COUNT(*) AS spravki_count, SUM(price) AS total FROM spravki;

-- ============================================================
-- Шаг 5. Запросы для клиники
-- ============================================================

-- 1. регистратура: все пациенты по алфавиту
SELECT * FROM patients ORDER BY name;

-- 2. пациенты, родившиеся в 2000 году и позже
SELECT * FROM patients WHERE birth_date >= '2000-01-01' ORDER BY birth_date;

-- 3а. пациенты без телефона, НЕПРАВИЛЬНО: ничего не находит.
-- NULL значит «неизвестно». phone = NULL даёт не true и не false, а тоже NULL (неизвестно),
-- а WHERE оставляет только строки, где условие true. Поэтому ни одна строка не подходит
SELECT * FROM patients WHERE phone = NULL;

-- 3б. пациенты без телефона, ПРАВИЛЬНО: для проверки на пустоту есть специальный оператор IS NULL
SELECT * FROM patients WHERE phone IS NULL;

-- 4а. поиск по части имени через LIKE: учитывает регистр.
-- Находит «Жылдыз Найманова» и «айжан Ормонова» (маленькая «ай»), но НЕ находит «Айбек» и «Айпери»
SELECT * FROM patients WHERE name LIKE '%ай%';

-- 4б. поиск через ILIKE (I = insensitive): без учёта регистра, находит и «Айбек», и «Айпери», и «айжан»
SELECT * FROM patients WHERE name ILIKE '%ай%';

-- 5. карточка пациента: все справки пациента с id 1 с названием, ценой и датой
SELECT spravka_types.name AS spravka, spravki.price, spravki.issued_at
FROM spravki
JOIN spravka_types ON spravki.spravka_type_id = spravka_types.id
WHERE spravki.patient_id = 1
ORDER BY spravki.issued_at;

-- 6. справки, выписанные в ноябре 2026
SELECT * FROM spravki
WHERE issued_at >= '2026-11-01' AND issued_at < '2026-12-01'
ORDER BY issued_at;

-- 7. самая дорогая справка в каталоге
SELECT * FROM spravka_types ORDER BY price DESC LIMIT 1;

-- 8. главврач: сколько справок выписано в ноябре 2026 и на какую сумму
SELECT COUNT(*) AS spravki_count, SUM(price) AS total
FROM spravki
WHERE issued_at >= '2026-11-01' AND issued_at < '2026-12-01';

-- 9а. каталог подорожал: Справка 083 теперь стоит 150 вместо 100
UPDATE spravka_types SET price = 150 WHERE name = 'Справка 083';

-- 9б. проверка: в каталоге новая цена 150...
SELECT * FROM spravka_types WHERE name = 'Справка 083';

-- 9в. ...а уже выданные справки 083 (вид 1) остались по 100: цена продажи хранится в самой справке
SELECT * FROM spravki WHERE spravka_type_id = 1;

-- 10. удалить одну справку (справка 12, выписанная по ошибке)
DELETE FROM spravki WHERE id = 12;

-- 11. бонус: сколько справок у каждого пациента и на какую сумму, включая пациентов без справок (LEFT JOIN)
SELECT patients.id, patients.name, COUNT(spravki.id) AS spravki_count, COALESCE(SUM(spravki.price), 0) AS total
FROM patients
LEFT JOIN spravki ON spravki.patient_id = patients.id
GROUP BY patients.id, patients.name
ORDER BY spravki_count DESC, patients.name;

-- ============================================================
-- Шаг 6. База защищает данные (каждый запрос ниже заканчивается ошибкой)
-- ============================================================

-- справка несуществующему пациенту: нарушен внешний ключ spravki_patient_id_fkey
INSERT INTO spravki (patient_id, spravka_type_id, price) VALUES (999, 1, 100);

-- отрицательная цена в каталоге: нарушена проверка spravka_types_price_check
INSERT INTO spravka_types (name, price, valid_days) VALUES ('Справка в лагерь', -500, 30);

-- пациент без имени: нарушено ограничение NOT NULL для name
INSERT INTO patients (birth_date) VALUES ('2000-01-01');

-- удалить пациента, на которого ссылаются справки: нарушен внешний ключ
DELETE FROM patients WHERE id = 1;

-- номера с пропусками: добавляем справку правильно...
INSERT INTO spravki (patient_id, spravka_type_id, price) VALUES (3, 2, 750);

-- ...и смотрим её id: справок было 12, а новая получила 14, потому что номер 13 забрала
-- неудачная вставка с пациентом 999. Номера идут с пропусками, но никогда не повторяются
SELECT * FROM spravki WHERE patient_id = 3;
