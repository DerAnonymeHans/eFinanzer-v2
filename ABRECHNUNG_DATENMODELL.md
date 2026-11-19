# Abrechnung – Datenmodell

Was wir aus den Kassenzetteln / Abrechnungen (`abrechnung.pdf`) brauchen und was in der DB gespeichert werden muss.

Grundlage: `abrechnung.pdf` (Beispielabrechnung), Meeting-Notes 2026-08-20, bestehendes Schema (`AccountingUnit`, `FiscalYear`, `Receipt`, `Budget`).

---

## 1. Belegkopf (`Receipt`)

| Feld | Typ | Pflicht | Quelle / Beispiel | Anmerkung |
|---|---|---|---|---|
| `Id` | int (PK) | ja | – | surrogate key |
| `ReceiptNumber` | string | ja | „Abrechnung 27" | fortlaufend pro Haushaltsjahr, wird beim Anlegen vergeben |
| `ReceiptAt` | date | ja | 20.07.2026 | Abrechnungsdatum |
| `AccountingUnitKey` | string (FK) | ja | – | Buchungskreis (FSR, StuRa, …) |
| `FiscalYearKey` | string (FK) | ja | – | Haushaltsjahr des Belegs |
| `Type` | enum | ja | Standard | Standard / Haushaltsrate / Übertrag / Umbuchung |
| `AccountType` | enum | ja | Kasse | Kasse oder Konto – der Beleg bucht von hier ab |
| `Purpose` | string | ja | „Graduierungsfeier 2026" | Verwendungszweck |
| `CostCenter` | string | nein | „T7 - Graduiertenfeier" | Kostenstelle, optional |
| `RecipientName` | string | nein | „Verein für studentisches Leben Leipzig e.V." | Empfänger (Auszahlung) bzw. Absender (Einnahme) |
| `RecipientIban` | string | nein | „DE04 8604 …" | IBAN des Empfängers/Absenders |
| `ProtocolNumber` | string | nein | – | Protokollnummer, falls Beschluss nötig (>150 € ohne Verbrauchsgut) |
| `PaymentOrderCreated` | bool | ja (default false) | – | Status Z: Zahlungsanweisung erzeugt |
| `Printed` | bool | ja (default false) | – | Status AR: gedruckt & abgeheftet |
| `ProtocolAttached` | bool | ja (default false) | – | Status PR: Protokoll vorhanden |
| `LoanContract` | bool | ja (default false) | – | Status D: Darlehensvertrag |
| `CreatedBy` | string | ja | Keycloak-Username | wer die Abrechnung angelegt hat |
| `CreatedAt` / `UpdatedAt` | timestamp | ja | – | Audit |

**Nicht gespeichert:** Unterschriften (Kassenwart/Finanzer) – die ausgedruckte, unterschriebene Version ist verbindlich, digital wird nicht unterschrieben (Meeting-Entscheidung).

## 2. Belegpositionen (`ReceiptPosition`, neu)

Eine Abrechnung hat mehrere Positionen, getrennt in **Einnahmen** und **Ausgaben**.

| Feld | Typ | Pflicht | Quelle / Beispiel | Anmerkung |
|---|---|---|---|---|
| `Id` | int (PK) | ja | – | surrogate key |
| `ReceiptId` | int (FK) | ja | – | Zuordnung zum Beleg |
| `Kind` | enum | ja | Ausgabe | `Income` (Einnahme) oder `Expense` (Ausgabe) |
| `Amount` | decimal | ja | 692,50 € | Betrag (Gesamtbetrag der Position) |
| `DepositAmount` | decimal | nein | – | Pfand-Anteil, **separat ausgewiesen** und unten aufsummiert (Meeting-Entscheidung) |
| `Description` | string | ja | „Eichamt Getränke" | Beschreibung der Position |
| `ReceiptNumberRef` | string | nein | Belegnummer | bei Ausgaben: Verweis auf die Belegnummern (siehe Anhang im PDF) |
| `SortOrder` | int | ja | – | Reihenfolge in der Tabelle |

**Summen werden NICHT gespeichert**, sondern aus den Positionen berechnet:
- `Summe Einnahmen` = Σ `Amount` wo `Kind = Income`
- `Summe Ausgaben` = Σ `Amount` wo `Kind = Expense`
- `Ausgaben-Überhang` = Summe Ausgaben − Summe Einnahmen (= `Gesamtbetrag` im Beispiel: 451,00 €)
- `Summe Pfand` = Σ `DepositAmount` (separat)

## 3. Empfänger (optional, später)

Für das Empfänger-Autofill (letzte 5 Empfänger vorschlagen, Daten übernehmen):

| Feld | Typ | Anmerkung |
|---|---|---|
| `Id` | int (PK) | |
| `AccountingUnitKey` | string (FK) | Empfänger sind pro Haushalt |
| `Name` | string | z. B. „Verein für studentisches Leben Leipzig e.V." |
| `Iban` | string | |
| `LastUsedAt` | timestamp | für „letzte 5"-Vorschlag |

## 4. Typen von Belegen (laut README)

- **Standard** – normale Abrechnung
- **Haushaltsrate** – Rate aus dem Haushalt
- **Übertrag** – zu Beginn eines Haushaltsjahres, Überschüsse des Vorjahres; je ein Beleg für Kasse und einer für Konto
- **Umbuchung** – Kasse → Konto ergibt **zwei Abrechnungen** (eine pro AccountType)

## 5. Was die PDF-Ausgabe braucht (Typst-Template)

- Kopf: Abrechnungsnummer, Gesamtbetrag, Verwendungszweck, Kostenstelle, Abrechnungsdatum, Empfänger + IBAN
- Tabelle „Auflistung der Einnahmen" (Position, Betrag, Beschreibung)
- Tabelle „Auflistung der Ausgaben nach Belegnummern" (Position, Betrag, Beschreibung)
- Summen: Einnahmen, Ausgaben, Ausgaben-Überhang
- **Pfand als separate Zeile/Spalte**, aufsummiert
- Protokollnummer (falls vorhanden)
- Unterschriftsfelder: Kassenwart + Finanzer (leer zum Ausdrucken)
- **Mindestens 12 Positionszeilen** im Template (Kontoführungsgebühren: 1 Position pro Monat)

## 6. Regeln
- Jede Geldbuchung wird über Belege (`Receipt`) mit `ReceiptPosition`-Werten abgebildet – siehe README „Datenschema".
- Positiver Gesamtbetrag erhöht Kapital (Kasse/Konto), negativer verringert es. Positiver Pfandbetrag erhöht Pfandvermögen, negativer verringert es.
