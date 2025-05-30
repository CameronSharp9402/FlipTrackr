# FlipTrackr

**FlipTrackr** is a Windows desktop application designed to help resellers track inventory, manage expenses, and monitor profits with ease. Whether you're flipping part-time or running a full-time reselling operation, FlipTrackr provides a clean and powerful interface for staying on top of your business.

---

## Features

- **Inventory Management**  
  Add, edit, and track your products with fields like SKU, purchase price, selling price, stage, condition, and more.

- **Profit Calculation**  
  Automatically calculates your revenue, cost of goods, profit, ROI, gross margin, turnover rate, and more.

- **Reports & Charts**  
  Visualize your performance over time with interactive line charts filtered by date range and category.

- **Expense Tracking**  
  Record business-related expenses and factor them into your profit reporting.

- **File Attachments**  
  Attach images, receipts, invoices, or any relevant files to inventory items.

- **Sale Breakdown Popup**  
  When an item is marked as “Sold,” FlipTrackr automatically breaks down your earnings:  
  - 60% Reinvest  
  - 30% Pocket  
  - 10% Emergency Fund  
  (Based on your net earnings after fees and shipping.)

- **Modern UI with Light/Dark Theme**  
  Clean, responsive layout with optional dark mode toggle.

- **Advanced Filtering**  
  Quickly sort inventory and expenses with intuitive filtering options.

- **Local Data Storage**  
  All data is stored securely on your device — no internet connection or account required.

---

## Installation

Download the latest `.msi` installer from the [Releases](https://github.com/CameronSharp9402/FlipTrackr/releases) tab and run the setup.

> No additional configuration needed. FlipTrackr runs out of the box on Windows 10 and 11 (64-bit).

---

## Requirements

- Windows 10 or 11 (64-bit)
- .NET 6.0 or later (included in the installer)

---

## Cross-Platform Support

FlipTrackr uses [Avalonia XPF](https://avaloniaui.net/xpf) to enable future support for Mac and Linux builds.

> ⚠️ **Important for contributors:**  
> The `.csproj` and `NuGet.config` files required for Avalonia XPF are intentionally excluded from the public repository to comply with Avalonia’s licensing terms.  
> If you're evaluating or contributing to this project and need XPF access, you must obtain your own license and set up these files manually using the [official Avalonia XPF setup guide](https://docs.avaloniaui.net/docs/xpf/quickstart).

---

## Where Your Data Lives

Your data is stored locally at:  
`C:\Users\<YourName>\AppData\Local\FlipTrackr\`

This includes:
- `FlipTrackr.db` – your database
- `settings.txt` – your preferences
- `Attachments/` – any attached files

---

## Support & Donations

If you find FlipTrackr useful, consider supporting future development:  
[ko-fi.com/cameronsharp](https://ko-fi.com/cameronsharp)

---

## Coming Soon

- Marketplace-specific analytics  
- Custom category/tag management  
- User-requested feature integration

---

## Disclaimer

FlipTrackr is provided as-is with no guarantees. Always back up your data.  
Built with consideration by a fellow reseller.

---

## License

This project is licensed under the MIT License. See `LICENSE` for details.
