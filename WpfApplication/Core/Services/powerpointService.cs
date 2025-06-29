using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace VideoLecturer.Infrastructure.Services
{
    public interface IPowerPointService
    {
        List<string> GenerateSlidesFromPptx(string pptxPath, string outputFolder);
        string CreatePresentation(string title, string outputPath);
        void AddSlide(string pptxPath, string slideTitle, string content);
    }

    public class PowerPointService : IPowerPointService
    {
        private readonly string _slideImagesPath;

        public PowerPointService(string slideImagesPath)
        {
            _slideImagesPath = slideImagesPath;
        }

        public string CreatePresentation(string title, string outputPath)
        {
            using (var presentation = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation))
            {
                // Инициализация презентации
                var presentationPart = presentation.AddPresentationPart();
                presentationPart.Presentation = new P.Presentation(
                    new P.SlideMasterIdList(
                        new P.SlideMasterId { Id = 2147483648U, RelationshipId = "rId1" }),
                    new P.SlideIdList(),
                    new P.SlideSize { Cx = 9144000, Cy = 6858000 },
                    new P.NotesSize { Cx = 6858000, Cy = 9144000 },
                    new P.DefaultTextStyle());

                // Добавление мастер-слайда
                var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>("rId1");
                slideMasterPart.SlideMaster = new P.SlideMaster(
                    new P.CommonSlideData(
                        new P.ShapeTree(
                            new P.NonVisualGroupShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                                new P.NonVisualGroupShapeDrawingProperties(),
                                new P.ApplicationNonVisualDrawingProperties()),
                            new P.GroupShapeProperties(new D.TransformGroup()))),
                    new P.ColorMap(),
                    new P.SlideLayoutIdList(
                        new P.SlideLayoutId { Id = 2147483649U, RelationshipId = "rId1" }));

                // Добавление layout
                var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>("rId1");
                slideLayoutPart.SlideLayout = new P.SlideLayout(
                    new P.CommonSlideData(
                        new P.ShapeTree(
                            new P.NonVisualGroupShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                                new P.NonVisualGroupShapeDrawingProperties(),
                                new P.ApplicationNonVisualDrawingProperties()),
                            new P.GroupShapeProperties(new D.TransformGroup()),
                            new P.Shape(
                                new P.NonVisualShapeProperties(
                                    new P.NonVisualDrawingProperties { Id = 2, Name = "Title" },
                                    new P.NonVisualShapeDrawingProperties(new D.ShapeLocks() { NoGrouping = true }),
                                    new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = P.PlaceholderValues.Title })),
                                new P.ShapeProperties(),
                                new P.TextBody(
                                    new D.BodyProperties(),
                                    new D.ListStyle(),
                                    new D.Paragraph()))),
                    new P.ColorMapOverride(new D.MasterColorMapping())));

                // Добавление первого слайда
                AddDefaultSlide(presentationPart, title);

                presentation.Save();
                return outputPath;
            }
        }

        public void AddSlide(string pptxPath, string slideTitle, string content)
        {
            using (var presentation = PresentationDocument.Open(pptxPath, true))
            {
                var presentationPart = presentation.PresentationPart;
                var slidePart = AddNewSlide(presentationPart);
                var slide = slidePart.Slide;

                // Добавление заголовка
                var titleShape = slide.Descendants<P.Shape>()
                    .FirstOrDefault(s => s.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?
                        .PlaceholderShape?.Type?.Value == P.PlaceholderValues.Title);

                if (titleShape != null)
                {
                    var textBody = titleShape.GetFirstChild<P.TextBody>();
                    if (textBody == null)
                    {
                        textBody = new P.TextBody(
                            new D.BodyProperties(),
                            new D.ListStyle(),
                            new D.Paragraph(
                                new D.Run(
                                    new D.Text(slideTitle))));
                        titleShape.Append(textBody);
                    }
                    else
                    {
                        var paragraph = textBody.GetFirstChild<D.Paragraph>();
                        if (paragraph == null)
                        {
                            paragraph = new D.Paragraph();
                            textBody.Append(paragraph);
                        }

                        var run = paragraph.GetFirstChild<D.Run>();
                        if (run == null)
                        {
                            run = new D.Run(new D.Text(slideTitle));
                            paragraph.Append(run);
                        }
                        else
                        {
                            var text = run.GetFirstChild<D.Text>();
                            if (text == null)
                            {
                                run.Append(new D.Text(slideTitle));
                            }
                            else
                            {
                                text.Text = slideTitle;
                            }
                        }
                    }
                }

                // Добавление содержимого
                var contentShape = slide.Descendants<P.Shape>()
                    .FirstOrDefault(s => s.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?
                        .PlaceholderShape?.Type?.Value == P.PlaceholderValues.Body);

                if (contentShape != null)
                {
                    var textBody = contentShape.GetFirstChild<P.TextBody>();
                    if (textBody == null)
                    {
                        textBody = new P.TextBody(
                            new D.BodyProperties(),
                            new D.ListStyle(),
                            new D.Paragraph(
                                new D.Run(
                                    new D.Text(content))));
                        contentShape.Append(textBody);
                    }
                    else
                    {
                        var paragraph = textBody.GetFirstChild<D.Paragraph>();
                        if (paragraph == null)
                        {
                            paragraph = new D.Paragraph();
                            textBody.Append(paragraph);
                        }

                        var run = paragraph.GetFirstChild<D.Run>();
                        if (run == null)
                        {
                            run = new D.Run(new D.Text(content));
                            paragraph.Append(run);
                        }
                        else
                        {
                            var text = run.GetFirstChild<D.Text>();
                            if (text == null)
                            {
                                run.Append(new D.Text(content));
                            }
                            else
                            {
                                text.Text = content;
                            }
                        }
                    }
                }

                presentation.Save();
            }
        }

        private SlidePart AddNewSlide(PresentationPart presentationPart)
        {
            // Получаем шаблон для нового слайда
            var slideMasterPart = presentationPart.SlideMasterParts.First();
            var slideLayoutPart = slideMasterPart.SlideLayoutParts.First();

            // Создаем новый слайд
            var slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.Slide = new P.Slide(
                new P.CommonSlideData(
                    new P.ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new P.ApplicationNonVisualDrawingProperties()),
                        new P.GroupShapeProperties(new D.TransformGroup()),
                        new P.Shape(
                            new P.NonVisualShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 2, Name = "Title" },
                                new P.NonVisualShapeDrawingProperties(new D.ShapeLocks() { NoGrouping = true }),
                                new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = P.PlaceholderValues.Title })),
                            new P.ShapeProperties(),
                            new P.TextBody(
                                new D.BodyProperties(),
                                new D.ListStyle(),
                                new D.Paragraph())),
                        new P.Shape(
                            new P.NonVisualShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 3, Name = "Content" },
                                new P.NonVisualShapeDrawingProperties(new D.ShapeLocks() { NoGrouping = true }),
                                new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = P.PlaceholderValues.Body })),
                            new P.ShapeProperties(),
                            new P.TextBody(
                                new D.BodyProperties(),
                                new D.ListStyle(),
                                new D.Paragraph())))),
                new P.ColorMapOverride(new D.MasterColorMapping()));

            // Связываем с layout
            slidePart.AddPart(slideLayoutPart);

            // Добавляем слайд в презентацию
            uint newId = GetMaxSlideId(presentationPart.Presentation.SlideIdList) + 1;
            var slideId = new P.SlideId { Id = newId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
            presentationPart.Presentation.SlideIdList.Append(slideId);

            return slidePart;
        }

        private void AddDefaultSlide(PresentationPart presentationPart, string title)
        {
            var slidePart = AddNewSlide(presentationPart);
            var slide = slidePart.Slide;

            // Устанавливаем заголовок
            var titleShape = slide.Descendants<P.Shape>()
                .FirstOrDefault(s => s.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?
                    .PlaceholderShape?.Type?.Value == P.PlaceholderValues.Title);

            if (titleShape != null)
            {
                var textBody = titleShape.GetFirstChild<P.TextBody>();
                if (textBody == null)
                {
                    textBody = new P.TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(),
                        new D.Paragraph(
                            new D.Run(
                                new D.Text(title))));
                    titleShape.Append(textBody);
                }
            }

            presentationPart.Presentation.Save();
        }

        public List<string> GenerateSlidesFromPptx(string pptxPath, string outputFolder)
        {
            if (!File.Exists(pptxPath))
                throw new FileNotFoundException("PPTX file not found", pptxPath);

            var images = new List<string>();
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                // Используем PowerPoint для конвертации (если установлен)
                if (HasPowerPointInstalled())
                {
                    ConvertUsingPowerPoint(pptxPath, tempFolder);
                }
                else
                {
                    // Альтернативный метод конвертации (например, используя Aspose или другую библиотеку)
                    ConvertUsingAlternativeMethod(pptxPath, tempFolder);
                }

                // Переносим изображения в целевую папку
                foreach (var imagePath in Directory.GetFiles(tempFolder, "*.png"))
                {
                    var destPath = Path.Combine(outputFolder, Path.GetFileName(imagePath));
                    File.Move(imagePath, destPath, true);
                    images.Add(destPath);
                }

                return images.OrderBy(f => f).ToList();
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        private bool HasPowerPointInstalled()
        {
            try
            {
                var powerpointType = Type.GetTypeFromProgID("PowerPoint.Application");
                return powerpointType != null;
            }
            catch
            {
                return false;
            }
        }

        private void ConvertUsingPowerPoint(string pptxPath, string outputFolder)
        {
            var powerpointApp = Activator.CreateInstance(Type.GetTypeFromProgID("PowerPoint.Application"));
            try
            {
                dynamic presentation = powerpointApp.GetType().InvokeMember(
                    "Presentations", System.Reflection.BindingFlags.GetProperty, null, powerpointApp, new object[] { pptxPath });

                presentation.SaveAs(outputFolder, 18); // 18 - это ppSaveAsPNG
                presentation.Close();
            }
            finally
            {
                powerpointApp.GetType().InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, powerpointApp, null);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(powerpointApp);
            }
        }

        private void ConvertUsingAlternativeMethod(string pptxPath, string outputFolder)
        {
            // Реализация с использованием библиотеки (например, Aspose.Slides)
            // Это платная библиотека, поэтому оставлю заглушку
            throw new NotImplementedException("Alternative conversion method not implemented. Please install PowerPoint or use a conversion library.");
        }

        private uint GetMaxSlideId(SlideIdList slideIdList)
        {
            return slideIdList?.ChildElements.Cast<SlideId>().Max(x => x.Id) ?? 256U;
        }

        
    }
}