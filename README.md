# Critter Caravan Client
The Game client for Critter Caravan, built with Unity!

# Requirements

To run this Client, you will need Unity Version 6. This was last developed with editor version **6000.0.39f1**; so that is the recommended version to run this module.

We do not offer any prebuilt releases at this time.

# Running the Game

Before you open the game, make sure the server is running. By default, Unity will try to contact `http://localhost:8000` to talk to the server. You can change this by setting the `BASE_URL` and `BASE_URL_API` environment variables.

```
BASE_URL = http://127.0.0.1:8000/
BASE_URL_API = http://127.0.0.1:8000/api/
```
Ensure that they end with a slash (`/`), otherwise, dynamic route generation may fail.

Once the editor is open, you can start playing form the LoginScreen scene. Simply hit play, log in (or sign up), and you should be right in.
