from sqlalchemy.orm import Session
from app.models import Logs

def create_log_entry(db: Session, payload: dict, session_id: int, topic: str, iteration: int = 0):
    try:
        data = payload.get("data", {})

        new_log = Logs(
            session_id=session_id,
            topic=topic,
            iteration=iteration,  
            data=data
        )
        db.add(new_log)
        db.commit()
        db.refresh(new_log)
        print(f"💾 SAVED Log ID: {new_log.id}", flush=True)
    except Exception as e:
        print(f"❌ DB Error: {e}", flush=True)
        db.rollback()
        raise e

# READ (Used by Web Router)
def get_all_logs(db: Session, limit: int = 100):
    return db.query(Logs).order_by(Logs.timestamp.desc()).limit(limit).all()