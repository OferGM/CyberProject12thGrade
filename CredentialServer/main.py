import logging
import sys
import traceback
from server import CredentialServer

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('credential_server.log'),
        logging.StreamHandler()  # This will output to console as well
    ]
)
logger = logging.getLogger("CredentialServer")

def main():
    """Main entry point"""
    logger.info("Starting Credential Server...")
    
    try:
        # Load configuration and initialize server
        logger.debug("Initializing server with configuration")
        server = CredentialServer()
        
        # Start the server
        logger.debug("About to call server.start()")
        server.start()
        
    except KeyboardInterrupt:
        logger.info("Server interrupted. Shutting down...")
    except Exception as e:
        logger.error(f"Unhandled exception: {e}")
        logger.error(traceback.format_exc())
        print(f"ERROR: {e}")
        print("Check the log file for more details.")
        sys.exit(1)
    finally:
        try:
            server.stop()
            logger.info("Server stopped.")
        except Exception as e:
            logger.error(f"Error stopping server: {e}")


if __name__ == "__main__":
    main()