change temperature to 0.1 when calculating payroll





project overview

Create a model class InputOutputModel to hold the data for the view.

In the HomeController, define the Index action to display the main page. The page will have a textarea for user input (prompt) and a file upload control to upload CSV files.

Implement the AnalyzeFile action that handles the user input and file upload. It sends the data to the OpenAI API and receives the response, which is stored in the model.Response property.

Display the generated response in the view below the textarea.

Add a "Download CSV" link that triggers the DownloadResponse action. In this action, the generated response will be converted to a CSV file and returned for download.

Add a "Download PDFs" link that triggers the DownloadPdfs action. In this action, the generated response will be processed to generate PDF files for each row and return them as a ZIP file for download.

Create the GeneratePdfFromText method to generate PDF content from a given text.

In the DownloadPdfs action, extract the relevant data from the Response and process it as CSV data using CsvHelper. Convert each row of data into PDF content and save them to a temporary folder.

Create a ZIP archive and add all the generated PDF files to it.

Return the ZIP file for download.
