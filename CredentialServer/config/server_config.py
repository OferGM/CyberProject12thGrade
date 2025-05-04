import os
import configparser

def load_config(config_file: str = "server_config.ini") -> configparser.ConfigParser:
    """Load server configuration from file"""
    config = configparser.ConfigParser()
    
    # Default configuration
    config["Server"] = {
        "host": "0.0.0.0",  # Listen on all interfaces
        "port": "43456"     # Updated to match your internal port
    }
    
    config["Storage"] = {
        "type": "filesystem"  # or "mongodb"
    }
    
    config["FileSystem"] = {
        "directory": "credentials"
    }
    
    config["MongoDB"] = {
        "connection_string": "mongodb://localhost:27017/",
        "database": "credential_store",
        "collection": "credentials"
    }
    
    # Try to load from file, use defaults if not found
    if os.path.exists(config_file):
        config.read(config_file)
    else:
        # Save default config
        with open(config_file, 'w') as f:
            config.write(f)
    
    return config