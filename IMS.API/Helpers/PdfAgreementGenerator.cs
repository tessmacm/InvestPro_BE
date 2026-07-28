using System;
using System.Text;

namespace IMS.API.Helpers;

public static class PdfAgreementGenerator
{
    public static byte[] GenerateUnsignedAgreementPdf(
        string investorName,
        string investorEmail,
        string organization,
        string regNumber,
        decimal capitalAmount,
        string? bankName,
        string? accountNumber,
        string? sortCode,
        string projectName = "Current Operations",
        DateTime? onboardingDate = null)
    {
        var dateStr = (onboardingDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        var amountStr = $"GBP {capitalAmount:N2}";

        var streamText = new StringBuilder();
        streamText.AppendLine("BT");

        // Header
        streamText.AppendLine("/F1 18 Tf");
        streamText.AppendLine("50 740 Td");
        streamText.AppendLine("(INVESTPRO INVESTMENT AGREEMENT) Tj");

        streamText.AppendLine("0 -24 Td");
        streamText.AppendLine("/F2 11 Tf");
        streamText.AppendLine("(STATUS: UNSIGNED DRAFT - PENDING INVESTOR SIGNATURE) Tj");

        streamText.AppendLine("0 -16 Td");
        streamText.AppendLine($"(Date: {EscapePdf(dateStr)}   |   Ref: AG-INV-{DateTime.UtcNow:yyyyMMddHHmmss}) Tj");

        // Section 1: Investor & Commitment Details
        streamText.AppendLine("0 -24 Td");
        streamText.AppendLine("/F1 12 Tf");
        streamText.AppendLine("(1. INVESTOR & CAPITAL COMMITMENT DETAILS) Tj");

        streamText.AppendLine("0 -16 Td");
        streamText.AppendLine("/F2 10 Tf");
        streamText.AppendLine($"(Investor Name:       {EscapePdf(investorName)}) Tj");
        streamText.AppendLine("0 -14 Td");
        streamText.AppendLine($"(Investor Email:      {EscapePdf(investorEmail)}) Tj");
        streamText.AppendLine("0 -14 Td");
        streamText.AppendLine($"(Organization / Reg:  {EscapePdf(organization ?? "N/A")} \\(Reg: {EscapePdf(regNumber ?? "N/A")}\\)) Tj");
        streamText.AppendLine("0 -14 Td");
        streamText.AppendLine($"(Committed Capital:   {EscapePdf(amountStr)}) Tj");
        streamText.AppendLine("0 -14 Td");
        streamText.AppendLine($"(Designated Project:  {EscapePdf(projectName)}) Tj");
        streamText.AppendLine("0 -14 Td");
        streamText.AppendLine($"(Bank Details:        {EscapePdf(bankName ?? "N/A")} | A/C: {EscapePdf(accountNumber ?? "N/A")} | Sort: {EscapePdf(sortCode ?? "N/A")}) Tj");

        // Section 2: Terms & Conditions
        streamText.AppendLine("0 -24 Td");
        streamText.AppendLine("/F1 12 Tf");
        streamText.AppendLine("(2. TERMS & CONDITIONS) Tj");

        streamText.AppendLine("0 -16 Td");
        streamText.AppendLine("/F1 10 Tf");
        streamText.AppendLine("(Article 1: Capital Commitment & Portfolio Operations) Tj");
        streamText.AppendLine("0 -13 Td");
        streamText.AppendLine("/F2 9 Tf");
        streamText.AppendLine($"(This Agreement is executed between InvestPro Management and {EscapePdf(investorName)}.) Tj");
        streamText.AppendLine("0 -11 Td");
        streamText.AppendLine($"({EscapePdf(investorName)} commits capital of {EscapePdf(amountStr)} into {EscapePdf(projectName)}.) Tj");

        streamText.AppendLine("0 -16 Td");
        streamText.AppendLine("/F1 10 Tf");
        streamText.AppendLine("(Article 2: Return Disbursements & Auditing) Tj");
        streamText.AppendLine("0 -13 Td");
        streamText.AppendLine("/F2 9 Tf");
        streamText.AppendLine("(Earnings disbursements shall follow selected frequency metrics subject to audit verification.) Tj");

        streamText.AppendLine("0 -16 Td");
        streamText.AppendLine("/F1 10 Tf");
        streamText.AppendLine("(Article 3: Governing Law & Electronic Execution) Tj");
        streamText.AppendLine("0 -13 Td");
        streamText.AppendLine("/F2 9 Tf");
        streamText.AppendLine("(This document is governed by commercial laws. Digital signatures carry full legal force.) Tj");

        // Section 3: Signature Status
        streamText.AppendLine("0 -28 Td");
        streamText.AppendLine("/F1 11 Tf");
        streamText.AppendLine("(3. SIGNATURE STATUS) Tj");

        streamText.AppendLine("0 -16 Td");
        streamText.AppendLine("/F2 10 Tf");
        streamText.AppendLine("(Investor Signature:   [ PENDING SIGNATURE - NOT YET SIGNED ]) Tj");
        streamText.AppendLine("0 -14 Td");
        streamText.AppendLine("(Platform Signatory:   InvestPro Executive Management) Tj");

        streamText.AppendLine("ET");

        var contentBytes = Encoding.ASCII.GetBytes(streamText.ToString());

        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.4");

        var catalogOffset = pdf.Length;
        pdf.AppendLine("1 0 obj");
        pdf.AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
        pdf.AppendLine("endobj");

        var pagesOffset = pdf.Length;
        pdf.AppendLine("2 0 obj");
        pdf.AppendLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        pdf.AppendLine("endobj");

        var pageOffset = pdf.Length;
        pdf.AppendLine("3 0 obj");
        pdf.AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>");
        pdf.AppendLine("endobj");

        var font1Offset = pdf.Length;
        pdf.AppendLine("4 0 obj");
        pdf.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        pdf.AppendLine("endobj");

        var font2Offset = pdf.Length;
        pdf.AppendLine("5 0 obj");
        pdf.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        pdf.AppendLine("endobj");

        var streamOffset = pdf.Length;
        pdf.AppendLine("6 0 obj");
        pdf.AppendLine($"<< /Length {contentBytes.Length} >>");
        pdf.AppendLine("stream");
        pdf.Append(streamText.ToString());
        pdf.AppendLine("endstream");
        pdf.AppendLine("endobj");

        var xrefOffset = pdf.Length;
        pdf.AppendLine("xref");
        pdf.AppendLine("0 7");
        pdf.AppendLine("0000000000 65535 f ");
        pdf.AppendLine($"{catalogOffset:D10} 00000 n ");
        pdf.AppendLine($"{pagesOffset:D10} 00000 n ");
        pdf.AppendLine($"{pageOffset:D10} 00000 n ");
        pdf.AppendLine($"{font1Offset:D10} 00000 n ");
        pdf.AppendLine($"{font2Offset:D10} 00000 n ");
        pdf.AppendLine($"{streamOffset:D10} 00000 n ");

        pdf.AppendLine("trailer");
        pdf.AppendLine("<< /Size 7 /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine($"{xrefOffset}");
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string EscapePdf(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
