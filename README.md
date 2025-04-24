# ABX Mock Exchange Client (C#)

This project is a C# client application that communicates with the ABX Mock Exchange Server over TCP. It requests and receives binary packet data, identifies missing packets by sequence number, re-requests them, and produces a complete JSON file (`packets.json`) as output.

## 🛠 Requirements

- [.NET SDK](https://dotnet.microsoft.com/en-us/download) (version 6.0 or higher)
- [Node.js](https://nodejs.org/) (version 16.17.0 or higher)

## 🚀 Setup Instructions

1. **Clone this repository:**

   ```bash
   git clone https://github.com/AKSHAY89350/ABX_Mock_Exchanges.git
   cd ABX_Mock_Exchange
   ```

2. **Start the ABX Exchange Server:**

   > The mock server is provided in a zip file named `abx_exchange_server.zip`.

   ```bash
   cd abx_exchange_server
   node main.js
   ```

3. **Build and Run the C# Client:**

   Open a **new terminal window**, then:

   ```bash
   cd ABXClient
   dotnet build
   dotnet run
   ```

   You will be prompted to select:

   - `1`: Stream All Packets
   - `2`: Request Missing Packets

4. **Output:**

   - A JSON file named `packets.json` will be generated in the working directory.
   - All packets are printed in the console.

## 📂 Output Format

Each packet in the JSON output looks like this:

```json
{
  "Symbol": "MSFT",
  "BuySellIndicator": "B",
  "Quantity": 100,
  "Price": 25000,
  "PacketSequence": 1
}
```

## 📌 Notes

- The application ensures packets are received in complete sequence, requesting missing packets automatically.
- Big-endian format is handled during packet parsing.
- After requesting missing packets (Call Type 2), the client gracefully closes the TCP connection.

## 🧑‍💻 Author

Akshay Kumar

