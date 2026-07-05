using System;
using System.IO;
using GimnasioSolid.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GimnasioSolid.Services
{
    // Genera el comprobante de pago en PDF usando QuestPDF, con el logo y los datos de DeporVida Fitness definidos en GymBrandingSettings
    public sealed class ReceiptPdfGenerator : IReceiptPdfGenerator
    {
        public byte[] Generate(PaymentRecord payment)
        {
            if (payment is null)
            {
                throw new ArgumentNullException(nameof(payment));
            }

            var logoBytes = TryLoadLogo();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(11).FontColor("#102A43"));

                    page.Content().Column(column =>
                    {
                        // Encabezado con logo y datos del gimnasio
                        column.Item().Background("#141414").Padding(24).Row(row =>
                        {
                            if (logoBytes is not null)
                            {
                                row.ConstantItem(64).Image(logoBytes);
                            }

                            row.RelativeItem().PaddingLeft(16).Column(headerText =>
                            {
                                headerText.Item().Text(GymBrandingSettings.GymName).FontSize(20).Bold().FontColor(Colors.White);
                                headerText.Item().Text(GymBrandingSettings.Slogan).FontSize(10).FontColor("#C81E2C");
                            });

                            row.ConstantItem(170).Column(contact =>
                            {
                                contact.Item().AlignRight().Text(GymBrandingSettings.Phone).FontColor(Colors.White).FontSize(9);
                                contact.Item().AlignRight().Text(GymBrandingSettings.Email).FontColor(Colors.White).FontSize(9);
                            });
                        });

                        // ----- Cuerpo del comprobante -----
                        column.Item().Padding(28).Column(body =>
                        {
                            body.Item().Row(row =>
                            {
                                row.RelativeItem().Column(titleColumn =>
                                {
                                    titleColumn.Item().Text("COMPROBANTE DE PAGO").FontSize(24).Bold().FontColor("#C81E2C");
                                    titleColumn.Item().PaddingTop(4).Text($"N° {payment.ReceiptNumber}").FontSize(11).Bold();
                                    titleColumn.Item().Text($"Fecha: {payment.Date:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#64748B");
                                });

                                row.ConstantItem(200).Column(memberColumn =>
                                {
                                    memberColumn.Item().Text("MIEMBRO").Bold().FontSize(10).FontColor("#64748B");
                                    memberColumn.Item().Text(payment.MemberName).FontSize(13).Bold();
                                    memberColumn.Item().Text($"ID: {payment.MemberId}").FontSize(10);
                                    memberColumn.Item().Text($"Plan: {payment.PlanName}").FontSize(10);
                                });
                            });

                            body.Item().PaddingTop(24).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#C81E2C").Padding(8).Text("CONCEPTO").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#C81E2C").Padding(8).AlignRight().Text("MONTO").FontColor(Colors.White).Bold();
                                });

                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(8).Text($"Cuota mensual - {payment.PlanName}");
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(8).AlignRight().Text($"${payment.BaseAmount:0.00}");

                                if (payment.LateFee > 0)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(8).Text("Recargo por mora (pago atrasado)");
                                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(8).AlignRight().Text($"${payment.LateFee:0.00}");
                                }
                            });

                            body.Item().PaddingTop(20).Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor("#C81E2C").Padding(14).Column(payInfo =>
                                {
                                    payInfo.Item().Text("INFORMACIÓN DE PAGO").Bold().FontSize(11);
                                    payInfo.Item().PaddingTop(6).Text($"Método de pago: {payment.PaymentMethod}").FontSize(10);
                                    payInfo.Item().Text($"N° de comprobante: {payment.ReceiptNumber}").FontSize(10);
                                });

                                row.ConstantItem(200).PaddingLeft(20).Column(totals =>
                                {
                                    totals.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Subtotal").FontSize(10);
                                        r.ConstantItem(80).AlignRight().Text($"${payment.BaseAmount:0.00}").FontSize(10);
                                    });
                                    totals.Item().PaddingTop(4).Row(r =>
                                    {
                                        r.RelativeItem().Text("Mora").FontSize(10);
                                        r.ConstantItem(80).AlignRight().Text($"${payment.LateFee:0.00}").FontSize(10);
                                    });
                                    totals.Item().PaddingTop(8).BorderTop(1).BorderColor("#E2E8F0").PaddingTop(8).Row(r =>
                                    {
                                        r.RelativeItem().Text("TOTAL").Bold().FontSize(13);
                                        r.ConstantItem(80).AlignRight().Text($"${payment.Amount:0.00}").Bold().FontColor("#C81E2C").FontSize(14);
                                    });
                                });
                            });

                            body.Item().PaddingTop(30).Text("Gracias por confiar en DeporVida Fitness. Este comprobante es válido como constancia de pago.")
                                .FontSize(9).FontColor("#94A3B8").Italic();
                        });
                    });

                    // Pie de página
                    page.Footer().Background("#141414").Padding(16).Row(row =>
                    {
                        row.RelativeItem().Text(GymBrandingSettings.Address).FontColor(Colors.White).FontSize(9);
                        row.RelativeItem().AlignRight().Text($"{GymBrandingSettings.Phone}   ·   {GymBrandingSettings.Email}").FontColor(Colors.White).FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static byte[]? TryLoadLogo()
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, GymBrandingSettings.LogoRelativePath);
            return File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        }
    }
}
