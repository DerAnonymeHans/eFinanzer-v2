# eFinanzer

Dieses Projekt soll die Finanzverwaltung der SSV der HTWK und insbesondere die Arbeit von Finanzern und Kassenwarten vereinfachen.

## Systeme und Architektur

### Überblick
- <b>eFinanzer Backend</b> - C# Web API
- <b>eFinanzer Frontend</b> - Svelte Kit Web App
- <b>eFinanzer DB</b> - Postgres DB für die relationalen Applikationsdaten
- <b>Keycloak</b> - Nutzer und Rollen Management. Schnittstelle für den Shibboleth SSO
- <b>Keycloak DB</b> - Keycloak eigene postgres DB
- <b>Nginx</b> - Reverseproxy. Alle eingehenden Anfragen aus dem Web gehen über den Proxy und werden auf die korrekten Services (nach Pfad) aufgeteilt
- <b>Victorialogs</b> - Log DB und Frontend
- <b>Vector</b> - sammelt die Containerlogs von den DBs und Keycloak und loggt sie zu Victorialogs

Jedes dieser Systeme wird mittels docker compose in einem eigenen Container deployed.

### eFinanzer Backend
Die API ist der zentrale Bestandteil der Software. Das FE kommuniziert mit der API, die API kommuniziert mit der eFinanzer DB, Keycloak und Victorialogs. Weitere Informationen finden sich in der Readme im Ordner des Backends.

### Datenschema (eFinanzer DB)

Wichtige Begriffe des Datenschemas:
- <b>AccountingUnit</b> - Buchungskreis / Haushalt, z.B. ein FSR oder der StuRa. Für jeden öffentlichen Haushalt gibt es einen Buchungskreis
- <b>FiscalYear</b> - Finanzjahr. Jede AccountingUnit hat EIGENE FiscalYears. Jeder Beleg wird einem FiscalYear zugewiesen
- <b>Receipt</b> - Abrechnung / Belegkopf. JEDE Geldbuchung wird mit Belegen dargestellt, die Geldwerte stehen in den ReceiptPositions des Belegs. Ein Beleg hat entweder den Typ Kasse oder Konto - die Geldbuchung wird vom entsprechenden Typ abgebucht. Im folgenden die verschiedenen Typen von Belegen (nur Unterscheidung in Darstellung, funktional identisch):
  - Standard - Normale Abrechnung
  - Haushaltsrate
  - Übertrag - zu Beginn eines FiscalYears werden die Überschüsse des vorherigen FiscalYears auf das neue gebucht. Es gibt jeweils einen Beleg für die Kasse und einen für das Konto
- <b>ReceiptPosition</b> - die BelegPositionen tragen die Geldwerte eines Belegs. Sie haben getrennte Geldwerte für den Gesamtbetrag und den Pfandbetrag. Ein positiver Gesamtbetrag erhöht das Kapital (Kasse oder Konto) der AccountingUnit, ein negativer verringert es. Ein positiver Pfandbetrag erhöht das Pfandvermögen der AccountingUnit, ein negativer verringert es. Typische Beispiele:
  - Kauf eines Sterni Kastens: Gesamtbetrag=-15€ (Preis des Biers, inklusive Pfad), Pfandbetrag=4€ (von den 15€ sind 4€ Pfand, welche auf das Pfandvermögen der AccountingUnit addiert werden)
  - Pfandrückgabe: Gesamtbetrag=4€ (Einnahmen durch Pfandrückgabe), Pfandbetrag=-4€ (Pfandvermögen reduziert sich)
- <b>Budget</b> - Haushaltsplan. Jedes Budget wird einem FiscalYear zugeordnet. Ein FiscalYear kann mehrere Budgets haben (Budget einer AccountingUnit kann sich im Laufe des Jahres ändern)
- <b>BudgetGroup</b> - Gruppe von BudgetPositionen. Hat keinen eigenen Budget Wert, sondern gruppiert nur ein oder mehrere BudgetPositionen
- <b>BudgetPosition</b> - Haushaltstopf. Hat einen Wert für geplante Einnahmen und geplante Ausgaben


### Rechte- und Nutzermanagement (Keycloak)

Keycloak ist ein OpenSource Rechte- und Nutzermanagement System. Es wird der efinanzer realm verwendet.

#### Shibboleth Anbindung
Damit Nutzer keine eigenen Accounts für den eFinanzer anlegen müssen, wird Shibboleth (die Anmeldeplattform der HTWK) als SSO angeboten. Dafür wurde Shibboleth als Saml v2 Identity Provider in Keycloak angebunden. Damit Shibboleth den Redirect zu Keycloak erlaubt, musste ein Private-Public Key Pair hinterlegt und von der HTWK (Harald Wanke vom ITSZ) authorisiert werden.

Angemeldete Nutzer werden anschließend in Keycloak importiert. Importierte Daten: Vorname, Nachname, Username (eindeutig)

#### Rollen
Rollen werden mittels Gruppen abgebildet. Jeder nutzer kann in mehreren Gruppen sein. Folgende Gruppen existieren:

- accounting-units/
  - [accounting-unit-key]/
    - finance-officer/ - Finanzer eines AccountingUnits. Hat die meisten Rechte des Units
    - treasurer - Kassenwart eines AccountingUnits
    - finance-authorized-user - Finanzbevollmächtigter eines AccountingUnits
- administration/
  - finance-administration/ - Finanzverwaltung. Für die Verwaltung des eFinanzers zuständig 
  - head-finance-officer/ - zentraler Finanzer über alle AccountingUnits hinweg
  - finance-reviewer/ - zentrale Reviewer für alle AccountingUnits

  
## Deployment & CI/CD

Backend und Frontend werden unabhängig voneinander zu einem Docker Image gebaut, welches im Dockerhub hochgeladen wird. Beides passiert automatisch in der Pipeline beim push auf die main branch. Zusätzlich werden die für das Deployment notwendigen Dateien (docker-compose.yml, nginx.conf, vector.yaml) per SCP auf den Server kopiert. Docker compose wird anschließend neu gestartet.

### Keycloak
Keycloak wird zur Zeit nicht vollautomatisch konfiguriert. Bei Änderungen sind manuelle Container Neustarts möglich. Die Realm Konfiguration wird auch nicht automatisch übernommen, da dies bei Keycloak in Production gar nicht so einfach ist. Stattdessen muss der Realm manuell über die UI importiert werden, falls die Daten auf dem Server verloren gehen sollten.


### Nginx
Bei einer Änderung der nginx.conf muss der Nginx Container manuell neugestartet werden, damit die Änderungen übernommen werden.

## Local Development

Für lokale Entwicklung kann das docker compose im src/local_dev Ordner verwendet werden. Damit werden alle notwendigen externen Abhängigkeiten (Keycloak, DBs, VictoriaLogs) abgedeckt. Backend und Frontend werden lokal im Development Modus gestartet und mit den lokal gestarteten Containern verknüpft. Dazu mehr in den Readmes von FE und BE.