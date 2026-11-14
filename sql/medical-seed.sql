-- Задание 4. Каталог видов справок в базе medical (выполнять в базе medical, не clinic).
-- Имена от EF Core с большой буквы, поэтому в двойных кавычках
INSERT INTO "SpravkaTypes" ("Name", "Price", "ValidDays", "NeedsDoctorExam", "ForChildren") VALUES
    ('Справка 083', 100, 365, true, false),
    ('Санитарная книжка', 750, 365, true, false),
    ('Справка в бассейн', 300, 90, true, false),
    ('Справка 086 для поступления', 500, 180, true, false),
    ('Справка в детский лагерь', 200, 3, true, true),
    ('Справка в спортивную секцию', 250, 365, false, true);

-- проверка
SELECT * FROM "SpravkaTypes" ORDER BY "Id";
