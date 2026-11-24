# Auftaktmeeting eFinanzer – Zusammenfassung

**Datum:** 20.08.2026 · **Dauer:** ca. 51 min
**Teilnehmende:** Pascal (Entwicklung eFinanzer), Ben (Finanzer), Barbara (Finanzverwaltung)
**Quelle:** Gemini-Transkript `2026_08_20-Auftaktmeeting Ben und Barbara-Transkript.pdf`

> **Hinweis zur Quelle:** Das Transkript ordnet alle Aussagen „Pascal Setzer“ zu (die Sprechererkennung hat nicht funktioniert) und ist stellenweise stark verstümmelt. Wer was gesagt hat, ist daher nur aus dem Kontext abgeleitet. Unklare Stellen sind mit *(unklar)* markiert.

---

## Entscheidungen

| # | Entscheidung |
|---|---|
| 1 | **Haushaltsplan revisionssicher machen.** Jeder Stand bzw. jede Änderung wird gespeichert. Für den ersten Haushaltsplan und jeden Nachtragshaushalt wird ein PDF erzeugt, mit dem Vermerk „abgestimmt in der Sitzung vom …“. Derzeit lässt sich der Plan zu leicht ändern. |
| 2 | **Die unterschriebene, ausgedruckte Abrechnung ist die verbindliche Version.** Die digitale Abrechnung muss nicht unterschrieben werden. Eine hinterlegte digitale Unterschrift des Finanzers wurde besprochen, aber verworfen: Es ist besser, wirklich zu unterschreiben. |
| 3 | **Pfand wird in der Abrechnung separat ausgewiesen**, als eigene Zeile oder Spalte, die unten aufsummiert wird. |
| 4 | **Die Protokollnummer kommt auf die Abrechnung**, wenn ein Beschluss nötig war. |
| 5 | **Rollout zum Haushaltsjahr 2027 (01.01.2027)**, mit einer Schulung für die aktuellen Finanzer. Ben kann schon vorher als Übung starten; bisherige Abrechnungen werden dann nachgetragen. |
| 6 | **Schrittweise Einführung:** zuerst die Fachschaftsräte, weil dort oft Leute ohne Vorerfahrung sitzen. StuRa-spezifische Funktionen kommen später. |
| 7 | **Wegen Hosting und Server klärt Pascal das mit Heiko.** Ben bekommt Heikos Kontakt über WhatsApp. |

## Kernerkenntnisse

- **Der Kernnutzen ist „alles gebündelt, nichts geht verloren“.** Haushalt, Abrechnungen, Anträge, Protokolle und Dokumente liegen an einem Ort. Auch neue Finanzer haben so sofort den Überblick.
- **Die Haushaltstabelle wird nicht gespeichert, sondern live aus den Abrechnungen berechnet.** Nur Abrechnungen werden persistiert. Gewünscht ist trotzdem ein Export der Tabelle als Excel oder PDF zum Ausdrucken bzw. zur Kontrolle.
- **Status-Markierungen helfen bei der Kontrolle enorm.** Pascals Excel nutzt Kürzel pro Buchung: **Z** = Zahlungsanweisung vorhanden, **AR** = Abrechnung erstellt und gedruckt, **D** = Darlehensvertrag, **PR** = Protokoll vorhanden. Damit kann Barbara am Jahresende schnell prüfen. Das sollte in eFinanzer übernommen werden.
- **Kontrolle durch die Finanzverwaltung:** Der Endbetrag laut Bank und laut Kasse wird mit der Summe im System abgeglichen. Außerdem wird geprüft, ob die Ausgaben rechtmäßig sind, also ob z. B. Beschlüsse vorliegen.
- **Regel aus der Finanzordnung:** Ausgaben über 150 €, die keine Verbrauchsgüter sind, brauchen einen Finanzantrag und ein unterschriebenes Sitzungsprotokoll im Finanzordner. Deshalb sollen Protokolle hochgeladen und mit Finanzanträgen bzw. Abrechnungen verknüpft werden können.
- **Der StuRa ist deutlich komplexer als ein Fachschaftsrat.** Dazu gehören:
  - viele Töpfe und Untertöpfe, wodurch die Übersicht schnell unübersichtlich wird
  - **Sperrvermerke:** Ein gesperrter Topf darf nur nach einem Antrag genutzt werden.
  - **Deckungsfähigkeit:** Ein Topf deckt andere, sobald diese leer sind. Das wird am *gedeckten* Topf angegeben.
- **Datenschutz und Hosting:** Auf dem Server haben Admins immer Zugriff. Für den Einsatz beim StuRa wären weitere Anpassungen nötig.
- **Heiko** betreibt den Server ehrenamtlich, mit einer eigenen, auf Microsoft basierenden Infrastruktur. Für ihn soll so wenig Zusatzaufwand wie möglich entstehen.

## Besprochene Punkte im Detail

### Haushaltsplan und Töpfe
- Mittel zwischen Töpfen zu verschieben muss in der Sitzung beschlossen und festgehalten werden.
- Pro Topf soll neben dem Plan die tatsächliche Ausgabe stehen, summiert aus den Abrechnungen. Wird der Plan überschritten, wird der Wert rot.
- Nächstes Haushaltsjahr anlegen: Schon im Vorjahr soll der neue Plan neben dem laufenden geöffnet werden können.
- **Jahresübertrag:** Der Endstand des Vorjahres soll automatisch übernommen werden, wenn ein neues Haushaltsjahr angelegt wird. Wie genau das funktioniert, muss noch durchdacht werden.

### Finanzanträge
- Es gibt eine Übersicht mit Statussystem.
- Idee: Finanzanträge direkt mit der zugehörigen Abrechnung verknüpfen und das Protokoll dort hochladen.

### Abrechnungen und Zahlungsanweisungen
- Aus einer Abrechnung wird automatisch eine **Zahlungsanweisung** für die Kasse erzeugt, getrennt für Auszahlung und Einzahlung, und kann gedruckt werden.
- **Empfänger-Autofill:** Die letzten fünf Empfänger werden vorgeschlagen und ihre Daten übernommen. Idee: eine **Empfängerliste** für wiederkehrende Empfänger.
- **Empfänger/Absender** wird automatisch umgeschaltet, je nachdem ob es eine Ausgabe oder eine Einnahme ist (z. B. wenn die Fakultät Geld überweist). Spenden sind offiziell nicht vorgesehen.
- **Pfandrückgabe** wird als Einnahme verbucht.
- Eine **Umbuchung** von der Kasse aufs Konto ergibt zwei Abrechnungen.
- **Kontoführungsgebühren:** eine Abrechnung für alle Gebühren des Jahres, mit einer Position pro Monat. *Problem:* Die PDF-Vorlage hat zu wenige Positionszeilen. Philip hatte seine Vorlage auf 12 Zeilen erweitert.
- **Vorlagen:** Aktuell sind es Google-Docs-Vorlagen mit Platzhaltern. Die Vorlage wird kopiert, ausgefüllt und als PDF exportiert, was langsam ist. Offen ist, wo die Vorlagen liegen und ob jeder FSR sein Logo bzw. Bild austauschen kann. Das Format orientiert sich an der StuRa-Abrechnung, die mit Philip und Barbara abgestimmt wurde.
- Eigenbelege, z. B. bei verlorenem Kassenzettel, sind weniger wichtig.

### Pfand und Inventar
- Am Jahresende wird das Pfand gezählt. Pascal hat dafür eine Excel-Liste neben dem Pfandbuch, in die man nur die Anzahl einträgt und die den Wert ausrechnet.
- Pfand ist anrechenbares Kapital der Fachschaft und muss daher für den Übertrag gezählt werden. Ideal wäre, es vorher komplett abzugeben.
- Inventarliste und Schlüsselübergabe könnten später ins System. Bis dahin reicht das Word-Dokument.
- Eine Spalte für den Buchungstyp (Einzahlung, Übernahme, Ausgabe) ist nice to have.

### Jahresabschluss
- Ziel ist **ein Dokument**: eine Checkliste der abzugebenden Unterlagen (Abrechnungen, neuer und alter Haushaltsplan …), dazu ein Deckblatt, optional alles zusammengefasst als ein PDF.
- Zwei Varianten wurden diskutiert:
  - Alles am Jahresende drucken. Das bedeutet 50–100 Unterschriften auf einmal.
  - Jede Abrechnung sofort drucken und abheften. Dabei hilft ein Status, ob sie schon gedruckt ist.
- Die Finanzordnung und das Dokument „Hinweise FSR Finanzen“ sollen direkt verlinkt werden. Der Link hat sich geändert und kommt neu in die Gruppe.

### Zugang, Rechte und Hosting
- Betrieb zentral auf dem Server, damit alle nur eine URL aufrufen müssen.
- **Login mit HTWK-Zugangsdaten**. Im Meeting wurde die Technik nicht genannt; naheliegend wäre das bestehende Keycloak-Setup.
- **Rechtesystem:** Wer darf Rechte vergeben? Vorschlag: immer mindestens drei Personen mit Rechten, damit eine Vertretung möglich ist.
- **Personenbezogene Logins vs. Sprecher-Accounts:** Bisher werden geteilte Zugangsdaten von Sprecher zu Sprecher weitergegeben. Personenbezogene Logins sind sauberer, aber bei jedem Wechsel entsteht Aufwand.
- Idee: Anbindung an bestehende Tools von Lennard, z. B. Kalender und Abstimmungssystem *(unklar)*.
- Aktuell werden alle Dateien in einem Ordner und die Datenbank in einer Datei gespeichert. Ein Backup ist dadurch einfach, bei mehr Nutzern braucht es aber mehr.

### Randthemen
- **Online- vs. Papierüberweisungen:** Andere Fachschaften zahlen ca. 3 € pro Überweisungsbeleg. Online-Überweisungen werden aktuell von den Sprechern freigegeben. Eine Kontovollmacht bei der Sparkasse zu ändern, wenn Personen wechseln, ist aufwendig. Hier wurde nichts entschieden.
- **Aufwandsentschädigung für die Entwicklung (StuRa):** Sie gilt nur für das laufende Jahr; der Antrag müsste im Dezember gestellt werden. Normal gibt es 100 € pro Person, wegen des Aufwands ist evtl. eine Sonderregelung möglich *(Details unklar)*.
- **Weiterentwicklung mit Studierenden:** Pascal hat eine neue Tutorenstelle und könnte eFinanzer teilweise als echtes Projekt mit Studierenden weiterentwickeln, falls diese Interesse haben.
- Ein **How-To-Dokument** für Finanzer existiert und ist (fast) fertig. Es könnte eingebunden oder ergänzt werden.

## Action Items

| Wer | Was |
|---|---|
| Pascal | Haushaltsplan-Versionierung und PDF-Export mit Sitzungsdatum umsetzen |
| Pascal | Abrechnungs-PDF anpassen: Pfand separat, Protokollnummer, mehr Positionszeilen |
| Pascal | Status-Markierungen (Z / AR / D / PR) und Protokoll-Upload pro Abrechnung bzw. Antrag |
| Pascal | Export der Haushaltstabelle (Excel/PDF) |
| Pascal | Jahresabschluss-Checkliste und Jahresübertrag konzipieren |
| Pascal | Hosting, Rechte und HTWK-Login mit Heiko abstimmen |
| Pascal | Link zum Ordner mit der „Extended“-Excel-Version in die Gruppe posten |
| Ben | Heikos Kontakt (WhatsApp) an Pascal weitergeben |
| ? | Neuen Link zu „Hinweise FSR Finanzen“ in die Gruppe schreiben |
| Ben | Optional: eFinanzer schon jetzt testen und bisherige Abrechnungen nachtragen |
| Pascal + Barbara | Schulung für Finanzer vor dem 01.01.2027 planen |
| später | StuRa-Features: Sperrvermerke, Deckungsfähigkeit, Untertöpfe |
