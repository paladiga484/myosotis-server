from fastapi import FastAPI
from . import CheckClientVersion, GetTermsOfUseStateAll, SignInAsSteam, SignInAsGoogle, UpdateTermsOfUseVersion


def add_login_handler(app: FastAPI):
    app.post("/login/CheckClientVersion")(CheckClientVersion.handle)
    app.post("/login/GetTermsOfUseStateAll")(GetTermsOfUseStateAll.handle)
    app.post("/login/SignInAsSteam")(SignInAsSteam.handle)
    app.post("/login/SignInAsGoogle")(SignInAsGoogle.handle)
    app.post("/login/CheckTermsOfUseData")(GetTermsOfUseStateAll.handle)
    app.post("/login/UpdateTermsOfUseVersion")(UpdateTermsOfUseVersion.handle)
