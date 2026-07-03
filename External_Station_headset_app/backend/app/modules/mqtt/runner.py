import sys
import os
from .connection import create_client, get_connection_details
from .listener import on_message

# 2. Define the callback locally
def on_connect(client, userdata, flags, rc):
    if rc == 0:
        print("✅ MQTT Connected! Subscribing to wildcard topics...", flush=True)
        # Jednoduchý zápis přímo do connectu
        client.subscribe("/unitymap/discovery/#")
        client.subscribe("/unitymap/+/status/#")
        client.subscribe("/unitymap/+/logs/#")
        client.subscribe("/unitymap/+/webrtc/#")
    else:
        print(f"❌ Connection failed with code: {rc}", flush=True)

# Renamed function and added 'state_module' argument
def initialize_mqtt_client_and_run_listener(state_module):
    # 3. Create the client
    client = create_client()
    
    # Set the global client reference in app.state
    state_module.mqtt_client = client # <--- NEW LINE
    
    # 4. Attach BOTH callbacks
    client.on_connect = on_connect  # <--- The missing piece
    client.on_message = on_message  # <--- The listener logic
    
    # 5. Connect
    broker, port = get_connection_details()
    print(f"🔌 MQTT Thread: Connecting to ", flush=True)
    
    try:
        client.connect(broker, port, 60)
        # We don't verify subscribe here anymore
        
        # 6. Loop forever
        client.loop_forever()
        
    except Exception as e:
        print(f"❌ MQTT Connection Error: {e}", flush=True)