-- SQL Data Integrity Checks for Accounting System

-- 1. Invoice Totals vs. Line Items
-- This check ensures that the total of an invoice matches the sum of its line items.
SELECT
    i.SalesInvoiceId AS InvoiceID,
    i.TotalAmount AS InvoiceTotal,
    SUM(il.LineTotal) AS CalculatedTotal
FROM
    SalesInvoices i
JOIN
    SalesInvoiceItems il ON i.SalesInvoiceId = il.SalesInvoiceId
GROUP BY
    i.SalesInvoiceId, i.TotalAmount
HAVING
    ROUND(i.TotalAmount, 2) <> ROUND(SUM(il.LineTotal), 2);

-- 2. Purchase Invoice Totals vs. Line Items
-- This check ensures that the total of a purchase invoice matches the sum of its line items.
SELECT
    pi.PurchaseInvoiceId AS InvoiceID,
    pi.TotalAmount AS InvoiceTotal,
    SUM(pil.LineTotal) AS CalculatedTotal
FROM
    PurchaseInvoices pi
JOIN
    PurchaseInvoiceItems pil ON pi.PurchaseInvoiceId = pil.PurchaseInvoiceId
GROUP BY
    pi.PurchaseInvoiceId, pi.TotalAmount
HAVING
    ROUND(pi.TotalAmount, 2) <> ROUND(SUM(pil.LineTotal), 2);

-- 3. Ledger Balance (Double-Entry Journals)
-- This check is a placeholder for double-entry accounting checks.
-- The current schema does not seem to have a standard journal entry table with debits and credits.
-- If a journal entry table is added, a query like the one below would be useful.
-- SELECT
--     JournalId,
--     SUM(CASE WHEN Side = 'debit' THEN Amount ELSE -Amount END) as Balance
-- FROM
--     JournalEntries
-- GROUP BY
--     JournalId
-- HAVING
--     ROUND(Balance, 2) <> 0;

-- 4. Stock Balance
-- This check is a placeholder for stock balance checks.
-- A full stock balance check would require a more complex query involving stock movements.
-- A simple check for negative stock could be:
SELECT
    p.ProductId,
    p.ProductName,
    p.Stock
FROM
    Products p
WHERE
    p.Stock < 0;

-- 5. Tax Calculations
-- This check ensures that the tax amount on an invoice is calculated correctly.
-- The TaxRate is not stored in the SalesInvoices table, so we cannot directly verify the calculation.
-- If the tax rate were available, a query like this would be useful:
-- SELECT
--     i.SalesInvoiceId,
--     i.SubTotal,
--     i.TaxAmount,
--     i.TaxRate,
--     ROUND(i.SubTotal * i.TaxRate / 100, 2) AS CalculatedTax
-- FROM
--     SalesInvoices i
-- WHERE
--     i.TaxAmount <> ROUND(i.SubTotal * i.TaxRate / 100, 2);