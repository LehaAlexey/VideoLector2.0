# Приложение для проекта "Видеолектор"
## Описание:
Проект "Видеолектор" призван автоматизировать запись видеолекций по средствам генерации озвучки и видеоряда преподавателя. Он развертывается на базе TTS-модели Coqui XTTS v 2.0 и SDA-модели SadTalker. В данном репозитории находится пользовательское приложение, работающее с сервером генерации (размещен в другом репозитории, сылку см. ниже)

Для развертывания проекта, необходимо выполнить следующие шаги:

## Установка веб-сервера генерации
  - Перейдите на репозиторий локального веб-сервера: https://github.com/LehaAlexey/VideoLEcture-Server;
  - Проведите установку согласно инструкциям в репозитории.

## Установка пользовательского приложения
  - Скачайте и разместите текущий репозиторий проекта;
  - Соберите приложение (через visual-studio: откройте .sln в VS -> проект -> сборка -> собрать решение);

## Использование
- Для запуска приложения, откройте ранее собранный .exe файл приложения;
- Перед запуском генерации, необходимо запустить локальный сервер (через src/app.py);
- При экспорте, выберите директорию, в которую будет сохранен файл видеолекции.
   
