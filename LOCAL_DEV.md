# Local Development Guide

## Requirements
- dotnet 10 SDK
  - Windows Powershell: https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1
  - Bash: https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
  - On Linux: Be careful to also install the ASP.NET Core SDK (the Web development framework). Normally it is directly bundled in the SDK but not in all package managers
- Node 26
  - https://nodejs.org/en/download
- docker
  - https://docs.docker.com/desktop/
- a Database viewer (technically optional, but very much recommended)
  - eg. DBeaver https://dbeaver.io/download/?start


## Step for step

### Starting the docker compose

For external dependencies like keycloak, victorialogs or the databases, there exists a ``src/local_dev/docker-compose.yml`` which can be run with `docker compose up`. 

#### Potential Problems
- Docker is not running. When using Docker Desktop, start Docker Desktop and wait until it loaded -> this starts the docker daemon in the background. Else, start the docker daemon using the command line:
  - bash: ``sudo systemctl start docker``
  - mac: ``open -a Docker``
- Docker compose not found
  - Install docker compose separately
- Ports already in use (localhost:5432 is standard for the efinanzer DB, localhost:5532 for keycloak)
  - shutdown the other program that uses the port if possible
  - change the port here (search for every usage of that number and replace it)

### Running the frontend

Navigate into the ``src/eFinanzerApp/efinanzer-app`` folder. Run the `npm install` command to install all missing dependencies (this can take a while). Then run `npm run start` to start the frontend. Open `localhost:3000` in the browser. If you are redirected to the login (`localhost:5532)`, everything is working.

#### Potential Problems
- Node or npm too old
  - Update them
- Port 3000 (standard port of the frontend) is already in use
  - shutdown the other program using it if possible (this is the best option!)
  - change the port of the frontend to something else
    - in ``vite.config.ts`` in the server section, update the port
    - in ``src/eFinanzer/Web/appsettings.json`` update the port in `AllowedOrigins`
    - open the keycloak UI (by default localhost:5532)
      - In ``Clients``, open the `efinanzer-app` client and update the `Valid redirect URI` and `Web origins` fieldes
    - None of these changes should be commited, but they are still version tracked. So please try to use option 1
- You changed the keycloak port (no login page when opening the frontend)
  - Set the correct keycloak port in ``.env.development``

### Creating a user in keycloak

Open keycloak in the browser (by default localhost:5532) and login using the admin credentials set in the docker-compose.yml (by default ``admin`` and `admin`). Switch the realm in the top left from `master` to `efinanzer`. Click on the ``Users`` menu point on the left. Click `Add User`. Set `Username`, `First Name` and `Last Name` and then `Create`. In the User details, click `Credentials` and `Set password`. Set `temporary` to false. This user can then be used to login for the efinanzer application.

Using your newly created user you can login in the login screen when opening the frontend.

#### Potential Problems
- Login not working despite correct credentials?
  - You probably created the user in the wrong realm (`master` instead of `efinanzer`)


### Running the backend

- Navigate into the ``src/eFinanzer/Web`` folder
- Set credentials
  - Duplicate the ``appsettings.TEMPLATE.secrets.json`` file and name it to `appsettings.Development.secrets.json`
  - Set the correct database connection string. By default, the connection string in already set string is correct. If you changed the port, update it here.
  - Set the correct keycloak client secret. Open the keycloak admin UI (localhost:5532), navigate to the efinanzer realm and to the Clients view. Select the ``efinanzer-backend`` client and click on the `Credentials` tab. View the `Client Secret`. If it is just starts (`*********`) click `Regenerate`. Then copy the secret and paste it in to the secrets file.
- Run the backend
  - Run ``dotnet run``. If it logs something like `Listening on https://localhost:7128` everything is working
- Now open the frontend and login. Then open the developer tools. Click on your name in the menu on the bottom left (profile page). Check which errors occur. If Cors errors are logged in the console, see the Potential Problems page. If the backends responds with errors (eg. HTTP 500) everything is working for you.


#### Potential problems
- Cors problems
  - If you have changed the frontend port, validate that you have updated it in `src/eFinanzer/Web/appsettings.json` in the ``AllowedOrigins`` section.
  - If you are using firefox, use a chromium based browser instead.
- HTTPS problems
  - Check if a warning that the dotnet developer certificate is not trusted was logged when starting the application. If so, do the following steps
  - run ``dotnet dev-certs https --clean``, then `dotnet dev-certs https`, restart the application and check again
  - If it logs, the certificate is only partially applied, try to use the script from the following repo, if you are using Ubuntu, Arch or Fedora: ``https://github.com/BorisWilhelms/create-dotnet-devcert``
- running ``dotnet restore`` may help sometimes
- An error like ``Set the configuration flag System.Globalization.Invariant to true`` (linux only)
  - install the missing packages
    - arch: ``sudo pacman -S icu``
    - debian/ubuntu: ``sudo apt install libicu-dev``
- An error like ``set AllowMissingPrunePackageData to TRUE``
  - This will only occur if you have not used the download script from above but instead used another package manager.
  - Should no longer occur as AllowMissingPrunePackageData was set to true in ``Directory.Build.props``


### Applying the initial migration to the database 

- Stay inside the ``src/eFinanzer/Web`` folder
- Make sure the backend is no longer running
- Run ``dotnet ef migrations script``
  - If it says you need to run ``dotnet tool restore``, run it
- Copy the created SQL (all of it)
- Open a database viewer, in the following DBeaver
  - Connect to the locally running database
    - click the button on the top left, ``Create Connection``
    - Select postgresql, and set host, port, database, username and password to the correct values
    - click Test Connection. If it fails, check your connection settings again. If it works, create the connection
  - Create a sql script (in top menu bar, a file like icon)
  - Paste the SQL script
  - On the left of the sql script there are a few buttons. Run the third button (Run SQL script). The first button only runs one statement at a time


#### Potential Problems
- an error like ``The exception 'Value cannot be null. (Parameter 'Host')' was thrown``
  - Run ``dotnet ef migrations script -- --environment Development`` instead
  - Check if you have a typo in the filename `appsettings.Development.secrets.json` (casing matters)

  
### Checking if everything works

- Make sure docker-compose, the svelte frontend and the dotnet backend are all running
- Open the frontend in the browser and open the dev console
- Play around to see if everything is working
  - eg Click on your profile and create a Rollenänderungsantrag. If that works, you are good to go!