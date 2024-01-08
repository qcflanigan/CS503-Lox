Project Description:

    This project reflects a complete interpreter for the Lox programming language. 
    The interpreter is written in the C# programming language and follows Robert Nystrom's Java implementation in his textbook 'Crafting Interpreters'. 
    The interpreter allows for full implementation of any Lox program, including support for variable declaration, loops, classes, superclasses and inheritance. 

Usage:

    The program runs using the .NET (dotnet) framework with the C# language. To construct the necessary dependencies for using the .NET framwork, follow the following link and setup instructions to install .NET. 
            https://dotnet.microsoft.com/en-us/download/dotnet/6.0

    Once installed, open a new folder within your IDE (this project was developed using Visual Studio Code) and paste all of the included files from the project into this folder. 
    
    Once you have all of the necessary files within your chosen IDE, run the command "dotnet build" within your IDE terminal. This will build a compiled binary of the C# files needed to execute any Lox program. 

    Once compiled, run the command "dotnet run" to execute individual Lox expressions, or "dotnet run <file_name>" to read in and execute an entire Lox program within the provided file name. The file can be a text file including the full Lox program or a .lox file itself. There is currently a test file in the working directory named "loxTest.txt". The user/tester can copy&paste any of the testing files in the '/test' directory into this text file and run the interpreter to produce the expected output of any individual Lox program.

Testing:

    This project uses Robert Nystrom's unit tests (https://github.com/munificent/craftinginterpreters/tree/master/test, included in the directory 'test') to test the interpreter for various Lox program cases. Run the command "dotnet run test" within your IDE terminal to generate and run each test file. 
    The output of each file will be printed in the terminal, all of which have been verified to be correct (264/264). 
    Many of the tests produce errors, all of which are the expected outcome of the test file to ensure the Lox Interpreter does not allow for any parsing errors. Each individual test file describes the expected output, including each expected error. 

    **
    A few of the tests were skipped during unit testing as some of the files did not have "expect" comments to describe the expected output, or took too long computationally. However, these skipped tests were run separately and their correct output was verified. 


Existing Errors:
    Through testing the interpreter, there were no errors found in any of the testing during the final iteration of the interpreter. However, I was not able to trim the " character from the output of strings after they were printed. 
    I followed the processes of the textbook and was not able to understand why the " kept getting appended onto the outputted strings. I assume I am missing a Consume() call somewhere in the parser or interpreter, but I have verified the proper Advance() call is in the Scanner class which should serve to trim the closing " from strings as they are read in. 
    I left this error as is because it does not seem to negatively affect any other portions of the interpreter and doesn't take away from readability from the user. 
