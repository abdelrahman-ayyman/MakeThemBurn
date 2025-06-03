# MakeThemBurn

MakeThemBurn is a specialized Unified Host application designed to automate the deployment of software onto multiple microcontrollers simultaneously. Developed as part of an internship project at IDACO, this tool leverages Ethernet and Serial communication protocols to streamline the programming process for custom microcontrollers based on the Microchip PIC18F97J60.

## Features

- **Multi-Device Programming**: Automates the deployment of software to multiple microcontrollers at once.
- **Ethernet**: Uses UDP sockets for over-the-air programming.
- **HEX File Parsing**: Parses Intel HEX files to extract and deploy program data.
- **Checksum Verification**: Ensures data integrity by comparing transmitted and received checksums.
- **Logging**: Logs connection status and burn status for each microcontroller.
- **User-Friendly Interface**: Built with Windows Forms for easy configuration and monitoring.
- **Scalability**: Designed to replace manual programming methods, improving efficiency on production lines.

## Requirements

- **Software**:
  - .NET Framework 8.0 or late.
  - Windows operating system.
- **Hardware**:
  - Microcontrollers with Ethernet (e.g., PIC18F97J60).
  - Ethernet cables.
- **Tools**:
  - MPLAB X IDE (optional, for generating HEX files).

## Installation

1. **Clone the Repository**:
   ```sh
   git clone https://github.com/abdelrahman-ayyman/MakeThemBurn.git
   ```
2. **Open in Visual Studio**:
   - Launch Visual Studio and open the solution file (`UnifiedHoat.sln`).
3. **Build the Project**:
   - Build the solution to restore dependencies and compile the application.

## Usage

1. **Launch the Application**:
   - Run `UnifiedHoat.exe` from the build output directory.
2. **Load HEX File**:
   - Click "Open a File" to select an Intel HEX file containing the program to deploy.
   - The application will parse and display the data sections.
3. **Connect to Microcontrollers**:
   - Press on console, then connect from the dropdown.
   - Set the port number to  establish connections (default: 65500).
   - Click "Connect".
4. **Deploy the Program**:
   - Click "Write File" to transmit data in 1KB packets to the microcontrollers.
   - The application waits for acknowledgments from each device.
5. **Monitor Progress**:
   - View the log window from console dropdown for real-time updates.
   - Errors, if any, will be logged for troubleshooting.
6. **Verify Checksum**:
   - Post-deployment, the application requests a checksum from each microcontroller.
   - Compare it with the expected value to confirm successful programming.
7. **Reset Microcontrollers**:
   - Send a reset command to load the newly deployed program.

**Troubleshooting Tip**: Use Wireshark to monitor UDP packets on port 65500 for Ethernet communication issues.

## Developer Notes

- **Language and Framework**: Written in C# using Windows Forms.
- **Multithreading**: Employed for concurrent connection handling and data transmission while the UI runs on a different thread.
- **Singleton Pattern**: Ensures only one connection port is active at a time.
- **HEX Parsing**: Implements Intel HEX format specification for data extraction.
- **Custom Protocol**: Communicates with the bootloader using a company-defined command set.

For detailed implementation, refer to the source code and inline comments.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Submit a pull request with your changes, adhering to the project's coding standards.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
