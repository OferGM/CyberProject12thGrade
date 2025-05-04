import logging
from interfaces.document_namer import IDocumentNamer
from interfaces.credential_repository import ICredentialRepository
from interfaces.credential_processor import ICredentialProcessor
from interfaces.connection_handler import IConnectionHandler
from namers.timestamp_namer import TimestampDocumentNamer
from repositories.mongodb_repository import MongoDBCredentialRepository
from repositories.filesystem_repository import FileSystemCredentialRepository
from processors.credential_processor import CredentialProcessor
from handlers.socket_handler import SocketConnectionHandler
from config.server_config import load_config

logger = logging.getLogger("CredentialServer")

class CredentialServer:
    """Main server class that coordinates all components"""
    
    def __init__(self, config_file: str = "server_config.ini"):
        """Initialize the server with configuration"""
        self.config = load_config(config_file)
        self.document_namer = TimestampDocumentNamer()
        
        # Set up repository
        if self.config.get("Storage", "type").lower() == "mongodb":
            self.repository = MongoDBCredentialRepository(
                self.config.get("MongoDB", "connection_string"),
                self.config.get("MongoDB", "database"),
                self.config.get("MongoDB", "collection")
            )
        else:
            self.repository = FileSystemCredentialRepository(
                self.config.get("FileSystem", "directory"),
                self.document_namer
            )
        
        # Set up processor
        self.processor = CredentialProcessor(self.repository)
        
        # Set up connection handler
        self.connection_handler = SocketConnectionHandler(
            self.config.get("Server", "host"),
            int(self.config.get("Server", "port")),
            self.processor
        )
    
    def start(self):
        """Start the server"""
        logger.info("Starting Credential Server...")
        self.connection_handler.start()
    
    def stop(self):
        """Stop the server"""
        logger.info("Stopping Credential Server...")
        self.connection_handler.stop()