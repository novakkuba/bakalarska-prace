import csv
import io
import json
import zipfile
import traceback
from typing import Any
from fastapi import APIRouter, Depends, Body, HTTPException
from fastapi.responses import StreamingResponse
from sqlalchemy.orm import Session

from app.database import get_db
from app.models import Logs, Session as SessionModel, Patient, Device 

router = APIRouter()

@router.post("/{session_id}/zip")
def export_session_zip(
    session_id: int, 
    config_data: Any = Body(default=[]), # DEFAULT=[] zabrání chybě 422 z frontendu
    db: Session = Depends(get_db)
):
    try:
        session_info = db.query(SessionModel).filter(SessionModel.id == session_id).first()
        if not session_info:
            raise HTTPException(status_code=404, detail="Session nebyla nalezena")

        # Absolutně bezpečné získání pacienta a zařízení
        patient_label = "Unknown"
        if hasattr(session_info, "patient") and session_info.patient:
            patient_label = getattr(session_info.patient, "patient_code", "Unknown")
        
        device_label = "Unknown"
        if hasattr(session_info, "device") and session_info.device:
            device_label = getattr(session_info.device, "name", "Unknown")

        zip_buffer = io.BytesIO()

        with zipfile.ZipFile(zip_buffer, "a", zipfile.ZIP_DEFLATED, False) as zip_file:
            
            # Bezpečný export konfigurace
            config_str = json.dumps(config_data if config_data else [], indent=2, ensure_ascii=False)
            zip_file.writestr(f"session_{session_id}_config.json", config_str)

            # Získání logů z DB (odstranil jsem order_by(timestamp) pro jistotu)
            logs = db.query(Logs).filter(Logs.session_id == session_id).all()
            
            if logs:
                csv_buffer = io.StringIO()
                writer = csv.writer(csv_buffer)
                
                writer.writerow([
                    "Session ID", "Patient Code", "Device Name", "Time", 
                    "Iteration", "Total Iterations", "Game", "Difficulty", "Data (JSON)"
                ])
                
                for log in logs:
                    # PARSOVÁNÍ DAT JAKO TANK
                    raw_data = getattr(log, "data", {})
                    
                    # Pokud DB vrací data jako string, převedeme je
                    if isinstance(raw_data, str):
                        try:
                            raw_data = json.loads(raw_data)
                        except json.JSONDecodeError:
                            raw_data = {"raw_string": raw_data}

                    # Pokud je to z nějakého důvodu pole, vezmeme první prvek
                    if isinstance(raw_data, list) and len(raw_data) > 0:
                        raw_data = raw_data[0]
                    elif not isinstance(raw_data, dict):
                        raw_data = {}

                    # Bezpečně vytáhneme informace přes .get() (nespadne, když klíč chybí)
                    current_game = raw_data.get("game", "Unknown")
                    current_difficulty = str(raw_data.get("difficulty", ""))
                    current_total_iterations = str(raw_data.get("iterations", ""))

                    # Kopie dat pro zbytek JSONu (odstraníme to, co už máme ve sloupcích)
                    processing_data = dict(raw_data)
                    processing_data.pop("game", None)
                    processing_data.pop("difficulty", None)
                    processing_data.pop("iterations", None)
                    
                    # Bezpečné získání času a iterace přímo z tabulky Logs
                    log_time = str(getattr(log, "timestamp", "Unknown"))
                    # Pokud nemáš v DB tabulce Logs sloupec "iteration", zkusí ho vzít z JSONu
                    log_iter = str(getattr(log, "iteration", processing_data.get("iteration", "")))
                    
                    writer.writerow([
                        session_id,
                        patient_label,              
                        device_label,               
                        log_time,
                        log_iter,
                        current_total_iterations,
                        current_game, 
                        current_difficulty,
                        json.dumps(processing_data, ensure_ascii=False)
                    ])
                
                zip_file.writestr(f"session_{session_id}_logs.csv", csv_buffer.getvalue())

        zip_buffer.seek(0)
        return StreamingResponse(
            iter([zip_buffer.getvalue()]),
            media_type="application/zip",
            headers={"Content-Disposition": f"attachment; filename=session_{session_id}_export.zip"}
        )

    except Exception as e:
        # TOHLE TĚ ZACHRÁNÍ: Vypíše to do konzole serveru do posledního detailu, KDE to spadlo
        print("\n" + "="*50)
        print("KRITICKÁ CHYBA PŘI EXPORTU:")
        traceback.print_exc()
        print("="*50 + "\n")
        raise HTTPException(status_code=500, detail=str(e))