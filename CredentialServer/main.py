import logging
from server import CredentialServer

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,  # Change from INFO to DEBUG
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('credential_server.log'),
        logging.StreamHandler()  # This will output to console as well
    ]
)
logger = logging.getLogger("CredentialServer")

def main():
    """Main entry point"""
    print("Starting Credential Server...")
    
    server = CredentialServer()
    
    try:
        print("About to call server.start()...")
        server.start()
        print("Server.start() called successfully")
    except KeyboardInterrupt:
        print("Server interrupted. Shutting down...")
    finally:
        server.stop()
        print("Server stopped.")


if __name__ == "__main__":
    main()