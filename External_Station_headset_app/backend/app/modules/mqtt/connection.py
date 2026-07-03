import os
import ssl
import paho.mqtt.client as mqtt

def create_client():
    """
    Creates and configures the MQTT Client object.
    Does NOT connect yet, just prepares the settings.
    """
    client = mqtt.Client()

    # 1. Credentials
    user = os.getenv("MQTT_USERNAME")
    pw = os.getenv("MQTT_PASSWORD")
    if user and pw:
        client.username_pw_set(user, pw)
    
    # 2. TLS / Security
    if os.getenv("MQTT_USE_TLS") == 'True':
        client.tls_set(tls_version=ssl.PROTOCOL_TLSv1_2)

    return client

def get_connection_details():
    """Returns the Tuple: (Address, Port)"""
    broker = os.getenv("MQTT_BROKER_ADDRESS", "mqtt_broker")
    port = int(os.getenv("MQTT_BROKER_PORT", 1883))
    return broker, port