# Key Logger with Email Notification

This project is a simple key logger application that logs keystrokes and sends them via email. It runs in the background, capturing keystrokes and periodically sending them to a specified email address. Additionally, it captures URLs visited in web browsers and includes them in the email notification.

## Features

- **Key Logging**: Records all keystrokes typed on the keyboard.
- **Email Notification**: Sends the logged keystrokes to a specified email address periodically.
- **URL Tracking**: Tracks URLs visited in supported web browsers (Chrome, Firefox, Internet Explorer, Edge).
- **Screenshot Capture**: Captures screenshots periodically and includes them in the email notification.
- **Location Information**: Retrieves location information based on the user's IP address and includes it in the email notification.
- **Customization**: Environment variables can be used to configure the email address and password for sending notifications, as well as other settings such as log file paths and notification frequency.

## Getting Started

1. Clone the repository to your local machine.
2. Set the required environment variables `EMAIL_ADDRESS` and `EMAIL_PASSWORD` with your email credentials.
3. Compile the project and run the executable file.
4. The key logger will start running in the background, capturing keystrokes and sending email notifications periodically.

## Dependencies

- .NET Framework 4.5 or higher

## Configuration

You can customize the following settings by setting environment variables:

- `EMAIL_ADDRESS`: Your email address for sending notifications.
- `EMAIL_PASSWORD`: Your email password.
- `LOG_FILE_NAME`: Path to the log file.
- `ARCHIVE_FILE_NAME`: Path to the archived log file.
- `SCREENSHOT_FILE_NAME`: Path to the screenshot file.
- `INCLUDE_LOG_AS_ATTACHMENT`: Whether to include the log file as an attachment in the email.
- `INCLUDE_SCREENSHOT_AS_ATTACHMENT`: Whether to include the screenshot as an attachment in the email.
- `MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL`: Maximum log file length before sending an email notification.
- `MAX_KEYSTROKES_BEFORE_WRITING_TO_LOG`: Maximum number of keystrokes before writing to the log file.

## Usage

- Ensure that the application is running in the background to capture keystrokes and send email notifications.
- Use the application responsibly and in compliance with applicable laws and regulations.

## License

This project is licensed under the [MIT License](LICENSE).

---

