# eFinanzer Frontend

## Local Development

Der lokale Entwicklungsserver wird mit ``npm run start`` gestartet. In den `.env` Dateien werden die Entwicklungsvariablen angegeben. Standardmäßig sind die Variablen in `.env.development` an die lokale Entwicklung mit dem /src/local_dev docker compose angepasst und müssen nicht geändert werden. Diese .env Dateien sind Teil der Versionierung, da dort niemals Secrets stehen sollten.

### API Client Änderungen
Die API Schnittstelle für das eFinanzer BE wird maschinell erstellt mittels des von dem eFinanzer BE bereitgestelltem OpenApi Dokuments. Im FE muss dafür folgender Befehl aufgerufen werden ``npm run generate:api [OpenApi Dokument Url]``. In der lokalen Entwicklung ist die Url standardmäßig http://localhost:5114/openapi/v1.json

### UI Component Framework
Für UI Komponenten wird ``shadcn-svelte`` verwendet. Dieses ist nicht als npm Paket eingebunden, sondern die Komponenten werden heruntergeladen und in src/lib/ui eingefügt und sind damit Teil der Versionierung. Zum hinzufügen eines neuen Komponenten wird der folgende Befehl verwendet `npx shadcn-svelte@latest add [ComponentName]`. Die Befehle finden sich ebenfalls auf der shadcn-svelte Webseite für die einzelnen Komponenten.