"""server_config.py"""

import os
import logging
import configparser

logger = logging.getLogger("CredentialServer")

def load_config(config_file: str = "server_config.ini") -> configparser.ConfigParser:
    """Load server configuration from file"""
    config = configparser.ConfigParser()
    
    # Default configuration
    config["Server"] = {
        "host": "0.0.0.0",  # Listen on all interfaces
        "port": "43456"     # Updated port
    }
    
    config["Storage"] = {
        "type": "mongodb"  # Default to filesystem
    }
    
    config["FileSystem"] = {
        "directory": "credentials"
    }
    
    config["MongoDB"] = {
        "connection_string": "mongodb+srv://ofergmizrahi:xCK7aO6yAtTGc5te@logininfo.vytelui.mongodb.net/?retryWrites=true&w=majority",
        "database": "credential_store",
        "collection": "credentials"
    }
    
    # Try to load from file, use defaults if not found
    if os.path.exists(config_file):
        logger.info(f"Loading configuration from {config_file}")
        try:
            config.read(config_file)
            
            # Verify we loaded the MongoDB section properly
            storage_type = config.get("Storage", "type")
            logger.debug(f"Storage type from config: {storage_type}")
            
            if storage_type.lower() == "mongodb":
                conn_string = config.get("MongoDB", "connection_string")
                database = config.get("MongoDB", "database")
                collection = config.get("MongoDB", "collection")
                logger.debug(f"MongoDB database: {database}, collection: {collection}")
                # Mask the password in logs
                masked_conn = conn_string.replace("://", "://***:***@") if "://" in conn_string else conn_string
                logger.debug(f"MongoDB connection string: {masked_conn}")
            
        except configparser.Error as e:
            logger.error(f"Error parsing config file: {e}")
            logger.warning("Using default configuration")
    else:
        logger.warning(f"Config file {config_file} not found, creating with defaults")
        # Save default config
        try:
            with open(config_file, 'w') as f:
                config.write(f)
        except Exception as e:
            logger.error(f"Error creating default config file: {e}")
    
    return config