This is a basic windows forms app used to read the information on NSF funded projects. One of the ways in which the NSF exposes information is through archived downloadable JSON files. This bare bones windows forms app picks up the files and parses them into a database. The scripts for the corresponding tables are included in the project scripts folder.

================================================================================================================

Assumptions/Requirements

================================================================================================================

1) The app assumes you already have an SQL database in place. If not, I reccomend downloading and installing MySQL and MySQLWorkBench.
2) Download MySql database  and mysql workbench here: https://dev.mysql.com/downloads/
3) The app assumes all json files in the folder and sub folders are NSF award JSON files.
4) The app will read all json files in the path and one level down in any child folders but not recursively all the way down.

================================================================================================================

How to use - If you only want the App

================================================================================================================
1) Hit green button that  says "code"  in the upper right corner of the repository
2) Click download zip


<img width="433" height="331" alt="image" src="https://github.com/user-attachments/assets/793292d5-7330-4204-82dc-d496f752a1d1" />


3) Unzip the folder
4) Find the SQLCreateScripts folder and run the TableCreation.sql first. This is VERY important because the app assumes a db with a certain table structure
5) navigate to the folder called "published" NOT  "publishedApp"
6) Click on NSF_JSON_Reader.exe (it may  trigger windows defender) click details and run anyway) and input the details of your database like so:


<img width="734" height="360" alt="image" src="https://github.com/user-attachments/assets/dbf02b71-68a1-4f57-aa3e-b99d530b427d" />

In my case MySql  is running on my local machine so the server name is localhost. The name of my database schema is convergence so that's  what I input for database name

<img width="512" height="326" alt="image" src="https://github.com/user-attachments/assets/91a5a1b7-ee4e-4d86-bc51-7e3c90aebe35" />


Lastly I am using the local root user so that's what I use for user credentials. I recommend clicking the test DB connection button before clicking the load files button
to test connectivity between the app and database and avoid unexpected errors.


5) Set how big  of a batch you want to use  to enter awards into your database. Larger batches will make it run faster especially when you have to load 10s of thousands of awards for in a folder to load. Smaller batches are slower but more resillient should there be a failure. using batch size  0  enters awards individually, slowest route but perfect for catching problematic malformed JSON files
   
6) Enter  the file path for the APP to read from and click load files


