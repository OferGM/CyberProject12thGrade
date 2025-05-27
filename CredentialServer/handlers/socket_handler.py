"""socket_handler.py"""
import socket
import threading
import json
import logging
from interfaces.connection_handler import IConnectionHandler
from interfaces.credential_processor import ICredentialProcessor
from interfaces.cryptography_service import ICryptographyService
from cryptography.aes_cryptography_service import AESCryptographyService

logger = logging.getLogger("CredentialServer")

class SocketConnectionHandler(IConnectionHandler):
    """Socket-based implementation of IConnectionHandler with encryption support"""
    
    def __init__(self, host: str, port: int, processor: ICredentialProcessor, 
                 cryptography_service: ICryptographyService = None):
        """Initialize with host, port, credential processor, and optional crypto service"""
        self.host = host
        self.port = port
        self.processor = processor
        self.cryptography_service = cryptography_service or AESCryptographyService()
        self.server_socket = None
        self.running = False
        self.threads = []
    
    def start(self):
        """Start listening for connections"""
        print(f"Creating socket...")
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        print(f"Setting socket options...")
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        try:
            print(f"Binding to {self.host}:{self.port}...")
            self.server_socket.bind((self.host, self.port))
            print(f"Starting to listen...")
            self.server_socket.listen(1)
            print(f"Server successfully listening on {self.host}:{self.port}")
            self.running = True
            
            logger.info(f"Server started with encryption support, listening on {self.host}:{self.port}")
            
            while self.running:
                try:
                    client_socket, client_address = self.server_socket.accept()
                    client_thread = threading.Thread(
                        target=self.handle_client,
                        args=(client_socket, client_address)
                    )
                    client_thread.daemon = True
                    client_thread.start()
                    self.threads.append(client_thread)
                    
                    print(f"Accepted connection from {client_address[0]}:{client_address[1]}")
                    logger.info(f"Accepted connection from {client_address[0]}:{client_address[1]}")
                except Exception as e:
                    if self.running:  # Only log if we're still supposed to be running
                        logger.error(f"Error accepting connection: {e}")
        
        except Exception as e:
            logger.error(f"Error starting server: {e}")
        finally:
            self.stop()
    
    def stop(self):
        """Stop listening for connections"""
        self.running = False
        
        if self.server_socket:
            try:
                self.server_socket.close()
            except Exception as e:
                logger.error(f"Error closing server socket: {e}")
        
        # Wait for client threads to finish
        for thread in self.threads:
            if thread.is_alive():
                thread.join(timeout=1.0)
        
        logger.info("Server stopped")
    
    def handle_client(self, client_socket, client_address):
        """Handle an individual client connection with encryption support"""
        try:
            # Receive data
            buffer = b""
            while True:
                chunk = client_socket.recv(4096)
                if not chunk:
                    break
                buffer += chunk
                
                # Check if we have a complete message (look for end marker)
                if buffer.endswith(b"<END>"):
                    buffer = buffer[:-5]  # Remove the end marker
                    break
            
            # Process the received data
            if buffer:
                try:
                    data_str = buffer.decode('utf-8')
                    received_data = json.loads(data_str)
                    
                    # Check if data is encrypted
                    if isinstance(received_data, dict) and received_data.get("encrypted") == True:
                        logger.info("Received encrypted data, decrypting...")
                        encrypted_content = received_data.get("data", "")
                        
                        # Decrypt the data
                        decrypted_json = self.cryptography_service.decrypt(encrypted_content)
                        data = json.loads(decrypted_json)
                        logger.info("Data successfully decrypted")
                    else:
                        # Handle unencrypted data for backward compatibility
                        logger.warning("Received unencrypted data (backward compatibility mode)")
                        data = received_data
                    
                    # Get client IP
                    client_ip = client_address[0]
                    logger.info(f"Processing data from IP: {client_ip}")
                    
                    # Add client IP to the data structure directly
                    if "ApplicationInfo" not in data:
                        data["ApplicationInfo"] = {}
                    data["ApplicationInfo"]["client_ip"] = client_ip
                    
                    # Process and store the credentials
                    result = self.processor.process_credentials(data, client_ip)
                    
                    # Create response
                    response_data = {
                        "status": "success",
                        "message": "Credentials received and stored successfully",
                        "document_id": result.get("document_id", "")
                    }
                    
                    # Encrypt the response
                    response_json = json.dumps(response_data)
                    encrypted_response = self.cryptography_service.encrypt(response_json)
                    
                    # Send encrypted response
                    response = {
                        "encrypted": True,
                        "data": encrypted_response
                    }
                    
                    client_socket.sendall(json.dumps(response).encode('utf-8'))
                    
                    logger.info(f"Successfully processed encrypted credentials from {client_address[0]}:{client_address[1]}")
                
                except json.JSONDecodeError as e:
                    logger.error(f"Invalid JSON received from {client_address[0]}:{client_address[1]}: {e}")
                    error_response = {
                        "encrypted": False,
                        "status": "error",
                        "message": "Invalid JSON format"
                    }
                    client_socket.sendall(json.dumps(error_response).encode('utf-8'))
                
                except Exception as e:
                    logger.error(f"Error processing data from {client_address[0]}:{client_address[1]}: {e}")
                    error_response = {
                        "encrypted": False,
                        "status": "error", 
                        "message": f"Error processing credentials: {str(e)}"
                    }
                    client_socket.sendall(json.dumps(error_response).encode('utf-8'))
        
        except Exception as e:
            logger.error(f"Error handling client {client_address[0]}:{client_address[1]}: {e}")
        
        finally:
            client_socket.close()