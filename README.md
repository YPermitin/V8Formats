# V8Formats
Библиотеки на .NET для работы с форматами файлов платформы 1С:Предприятие 8.x

# Класс V8File
C помощью класса V8File реализована функциональность распаковки в файловую структуру и запаковки в форматы файлов платформы 1С:Предприятие файлов конфигураций (*.CF), внешний обработок (*.ERF) и внешних отчетов (*.ERT).
Возможности аналогичны утилите V8Unpack, написаной на C++ (http://infostart.ru/public/15695).

Доступны следующие методы:
- Inflate и Deflate - распаковка и запаковка произвольных блоков данных.
- Unpack и Pack - распаковка файла в файловую структуру с минимальной детализацией и запаковка соответственно.
- Parse и Build - распаковка файла в файловую структуру с большей детализацией по сравнению с предыдущими вариантами команд и запаковка соответственно.

# NUGET-пакет
Добавлен NUGET-пакет для быстрого добавления библиотеки в Ваш проект.
Ссылка: https://www.nuget.org/packages/V8Formats

# Конольная утилита
В качестве примера использования библиотеки добавлена консольная утилита со следующим списком доступных команд:

V8Formats Version 1.0 Copyright (c)
- YPermitin (ypermitin@yandex.ru) www.develplatform.ru
- PSPlehanov (psplehanov@mail.ru)

Unpack, pack, deflate and inflate 1C v8 file (*.cf),(*.epf),(*.erf)

V8FORMATS

- U[NPACK]     in_filename.cf     out_dirname
- PA[CK]       in_dirname         out_filename.cf
- I[NFLATE]    in_filename.data   out_filename
- D[EFLATE]    in_filename        filename.data
- E[XAMPLE]
- BAT
- P[ARSE]      in_filename        out_dirname
- B[UILD]      in_dirname         out_filename
- V[ERSION]

# Лицензия
Разработка распостраняется по лицензии MIT. Полный текст лицензии на английском и русском языке вы найдете в репозитории.

# Другие V8Unpack'еры
Данная разработка изначально создавалась на основе решения от Дениса Демидова disa_da2@mail.ru
(https://www.assembla.com/spaces/V8Unpack/wiki)

На базе этого решения создана более оптимизированная версия V8Unpack Сергеем Батановым @dmpas
(https://github.com/dmpas/v8unpack)

# TODO
В будущем добавявятся возможности работы с форматами файлов *.GRS (включая визуализацию) и *.MXL, а также оптимизация существующего кода в части использования памяти.
