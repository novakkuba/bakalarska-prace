import queue
import paho.mqtt.client as mqtt 


active_queues = {}

mqtt_client: mqtt.Client = None 

locked_devices = set()


def add_to_stream(device_id: str, data: dict): 
    if device_id in active_queues:
        q = active_queues[device_id]
        if q.qsize() > 100:
            try:
                q.get_nowait() 
            except queue.Empty:
                pass
        q.put(data)

