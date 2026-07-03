"""
Hlavní vstupní bod FastAPI backendu.
Inicializuje databázové tabulky, registruje API routy jednotlivých modulů a spouští MQTT klienta v asynchronním vlákně na pozadí.
"""

import threading
from fastapi import FastAPI
from app.database import engine
from app import models
from app import state  
from app.modules.mqtt.runner import initialize_mqtt_client_and_run_listener 
from app.modules.logs.routers import router as logs_router
from app.modules.monitoring.routers import router as monitoring_router
from app.modules.signaling.routers import router as signaling_router
from app.modules.controls.routers import router as controls_router
from app.modules.export import routers as export_router
from app.modules.pacients import routers as patients_router
from app.modules.devices import routers as devices_router
from app.modules.sessions import routers as sessions_router

from fastapi.middleware.cors import CORSMiddleware


models.Base.metadata.create_all(bind=engine)

app = FastAPI()

origins: list[str] = ["*"]


app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(logs_router, prefix="/api/logs", tags=["Logs"])
app.include_router(monitoring_router, prefix="/api", tags=["Monitoring"])
app.include_router(signaling_router)
app.include_router(controls_router)
app.include_router(export_router.router, prefix="/api/export", tags=["Export"])
app.include_router(patients_router.router, prefix="/api/patients", tags=["Patients"])
app.include_router(devices_router.router, prefix="/api/devices", tags=["Devices"])
app.include_router(sessions_router.router, prefix="/api/sessions", tags=["Sessions"])

@app.on_event("startup")
async def startup_event():
    print("🚀 Backend Starting...")

    # Start the listener in the background, which also initializes the global client
    # Pass app.state to the function so it can update app.state.mqtt_client
    t = threading.Thread(target=initialize_mqtt_client_and_run_listener, args=(state,), daemon=True)
    t.start()

@app.get("/")
def read_root():
    return {"status": "Running"}