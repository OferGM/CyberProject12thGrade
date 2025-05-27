"""server.py"""
import logging
from interfaces.document_namer import IDocumentNamer
from interfaces.credential_repository import ICredentialRepository
from interfaces.credential_processor import ICredentialProcessor
from interfaces.connection_handler import IConnectionHandler
from interfaces.cryptography_service import ICryptographyService
from namers.timestamp_namer import TimestampDocumentNamer
from repositories.mongodb_repository import MongoDBCredentialRepository
from repositories.filesystem_repository import FileSystemCredentialRepository
from processors.credential_processor import CredentialProcessor
from handlers.socket_handler import SocketConnectionHandler
from cryptography.aes_cryptography_service import AESCryptographyService
from config.server_config import load_config

logger = logging.getLogger("CredentialServer")

class CredentialServer:
    """Main server class that coordinates all components"""
    
    def __init__(self, config_file: str = "server_config.ini", cryptography_service: ICryptographyService = None):
        """Initialize the server with configuration and optional cryptography service"""
        logger.info("Initializing CredentialServer with encryption support")
        self.config = load_config(config_file)
        self.document_namer = TimestampDocumentNamer()
        self.cryptography_service = cryptography_service or AESCryptographyService()
        
        # Set up repository
        storage_type = self.config.get("Storage", "type").lower()
        logger.info(f"Storage type from config: '{storage_type}'")
        
        if storage_type == "mongodb":
            # Get MongoDB configuration
            connection_string = self.config.get("MongoDB", "connection_string")
            database = self.config.get("MongoDB", "database")
            collection = self.config.get("MongoDB", "collection")
            
            # Mask password in logs
            masked_conn = connection_string
            if "://" in connection_string and "@" in connection_string:
                parts = connection_string.split("@")
                auth_part = parts[0]
                if ":" in auth_part:
                    user_part = auth_part.split(":", 1)[0]
                    masked_conn = f"{user_part}:****@{parts[1]}"
            
            logger.debug(f"MongoDB database: {database}, collection: {collection}")
            logger.debug(f"MongoDB connection string (masked): {masked_conn}")
            
            try:
                logger.info("Initializing MongoDB repository")
                self.repository = MongoDBCredentialRepository(
                    connection_string,
                    database,
                    collection
                )
                logger.info("MongoDB repository initialized successfully")
            except Exception as e:
                logger.error(f"Error initializing MongoDB repository: {e}")
                logger.warning("Falling back to FileSystem repository")
                self.repository = FileSystemCredentialRepository(
                    self.config.get("FileSystem", "directory"),
                    self.document_namer
                )
        else:
            logger.info(f"Using FileSystem repository with directory '{self.config.get('FileSystem', 'directory')}'")
            self.repository = FileSystemCredentialRepository(
                self.config.get("FileSystem", "directory"),
                self.document_namer
            )
        
        # Set up processor
        self.processor = CredentialProcessor(self.repository)
        
        # Set up connection handler with cryptography service
        self.connection_handler = SocketConnectionHandler(
            self.config.get("Server", "host"),
            int(self.config.get("Server", "port")),
            self.processor,
            self.cryptography_service
        )
        
        logger.info("Server initialized with end-to-end encryption enabled")
    
    def start(self):
        """Start the server"""
        logger.info("Starting Credential Server with encryption...")
        self.connection_handler.start()
    
    def stop(self):
        """Stop the server"""
        logger.info("Stopping Credential Server...")
        self.connection_handler.stop()