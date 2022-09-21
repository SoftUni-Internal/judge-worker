# mysql-run-queries-and-check-database-single-db
for i in {01..256}; do
curl -X POST -H 'content-type: application/json' -d '{
   "id":23665299,
   "executionType":"tests-execution",
   "executionStrategy":"mysql-run-queries-and-check-database-single-db",
   "code":"CREATE table countries(
id INT AUTO_INCREMENT PRIMARY KEY,
name VARCHAR(45) NOT NULL 
);
CREATE table towns(
id INT AUTO_INCREMENT PRIMARY KEY,
name VARCHAR(45) NOT NULL, 
country_id INT NOT NULL,
CONSTRAINT fk_town_countries
FOREIGN KEY (country_id)
REFERENCES countries(id)
);
CREATE table stadiums(
id INT AUTO_INCREMENT PRIMARY KEY,
name VARCHAR(45) NOT NULL, 
capacity INT NOT NULL,
town_id INT NOT NULL,
CONSTRAINT fk_stadiums_towns
FOREIGN KEY (town_id)
REFERENCES towns(id)
);

CREATE table teams(
id INT AUTO_INCREMENT PRIMARY KEY,
name VARCHAR(45) NOT NULL, 
established DATE NOT NULL,
fan_base BIGINT NOT NULL DEFAULT 0,
stadium_id INT NOT NULL,
CONSTRAINT fk_stadiums_team
FOREIGN KEY (stadium_id)
REFERENCES stadiums(id)
);
CREATE table skills_data(
id INT AUTO_INCREMENT PRIMARY KEY,
dribbling INT DEFAULT 0,
pace INT DEFAULT 0,
passing INT DEFAULT 0,
shooting INT DEFAULT 0,
speed INT DEFAULT 0,
strength INT DEFAULT 0
);
CREATE table coaches(
id INT AUTO_INCREMENT PRIMARY KEY,
first_name VARCHAR(10) NOT NULL,
last_name VARCHAR(20) NOT NULL,
salary DECIMAL(10, 2) NOT NULL DEFAULT 0, 
coach_level INT NOT NULL DEFAULT 0
);
CREATE table players (
id INT AUTO_INCREMENT PRIMARY KEY,
first_name VARCHAR(10) NOT NULL,
last_name VARCHAR(20) NOT NULL,
age INT NOT NULL DEFAULT 0,
position CHAR(1), 
salary DECIMAL(10, 2) NOT NULL DEFAULT 0, 
hire_date DATETIME NOT NULL,
skills_data_id INT NOT NULL,
team_id INT, 
CONSTRAINT fk_p_teams
FOREIGN KEY (team_id)
REFERENCES teams(id),
CONSTRAINT fk_p_skilla
FOREIGN KEY (skills_data_id)
REFERENCES skills_data(id)
);
CREATE table players_coaches (
player_id INT,
coach_id INT,
CONSTRAINT fk_maping_player
FOREIGN KEY (player_id)
REFERENCES players(id),
CONSTRAINT fk_maping_coaches
FOREIGN KEY (coach_id)
REFERENCES coaches(id));",
   "timeLimit":8000,
   "memoryLimit":16777216,
   "executionDetails":{
      "maxPoints":40,
      "checkerType":"trim",
      "tests":[
         {
            "id":189128,
            "input":"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS\r\nWHERE TABLE_SCHEMA = DATABASE()\r\nORDER BY TABLE_NAME, COLUMN_NAME;",
            "output":"coach_level\r\nfirst_name\r\nid\r\nlast_name\r\nsalary\r\nid\r\nname\r\nage\r\nfirst_name\r\nhire_date\r\nid\r\nlast_name\r\nposition\r\nsalary\r\nskills_data_id\r\nteam_id\r\ncoach_id\r\nplayer_id\r\ndribbling\r\nid\r\npace\r\npassing\r\nshooting\r\nspeed\r\nstrength\r\ncapacity\r\nid\r\nname\r\ntown_id\r\nestablished\r\nfan_base\r\nid\r\nname\r\nstadium_id\r\ncountry_id\r\nid\r\nname",
            "isTrialTest":true,
            "orderBy":1.0
         },
         {
            "id":189129,
            "input":"SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS\r\nWHERE TABLE_SCHEMA = DATABASE()\r\nORDER BY TABLE_NAME,COLUMN_TYPE;",
            "output":"decimal(10,2)\r\nint\r\nint\r\nvarchar(10)\r\nvarchar(20)\r\nint\r\nvarchar(45)\r\nchar(1)\r\ndatetime\r\ndecimal(10,2)\r\nint\r\nint\r\nint\r\nint\r\nvarchar(10)\r\nvarchar(20)\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nvarchar(45)\r\nbigint\r\ndate\r\nint\r\nint\r\nvarchar(45)\r\nint\r\nint\r\nvarchar(45)",
            "isTrialTest":true,
            "orderBy":2.0
         },
         {
            "id":189131,
            "input":"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS\r\nWHERE TABLE_SCHEMA = DATABASE()\r\nORDER BY COLUMN_NAME;\r\n",
            "output":"age\r\ncapacity\r\ncoach_id\r\ncoach_level\r\ncountry_id\r\ndribbling\r\nestablished\r\nfan_base\r\nfirst_name\r\nfirst_name\r\nhire_date\r\nid\r\nid\r\nid\r\nid\r\nid\r\nid\r\nid\r\nlast_name\r\nlast_name\r\nname\r\nname\r\nname\r\nname\r\npace\r\npassing\r\nplayer_id\r\nposition\r\nsalary\r\nsalary\r\nshooting\r\nskills_data_id\r\nspeed\r\nstadium_id\r\nstrength\r\nteam_id\r\ntown_id",
            "isTrialTest":false,
            "orderBy":1.0
         },
         {
            "id":189132,
            "input":"SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS\r\nWHERE TABLE_SCHEMA = DATABASE()\r\nORDER BY COLUMN_TYPE;",
            "output":"bigint\r\nchar(1)\r\ndate\r\ndatetime\r\ndecimal(10,2)\r\ndecimal(10,2)\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nvarchar(10)\r\nvarchar(10)\r\nvarchar(20)\r\nvarchar(20)\r\nvarchar(45)\r\nvarchar(45)\r\nvarchar(45)\r\nvarchar(45)",
            "isTrialTest":false,
            "orderBy":2.0
         },
         {
            "id":189132,
            "input":"SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS\r\nWHERE TABLE_SCHEMA = DATABASE()\r\nORDER BY COLUMN_TYPE;",
            "output":"bigint\r\nchar(1)\r\ndate\r\ndatetime\r\ndecimal(10,2)\r\ndecimal(10,2)\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nvarchar(10)\r\nvarchar(10)\r\nvarchar(20)\r\nvarchar(20)\r\nvarchar(45)\r\nvarchar(45)\r\nvarchar(45)\r\nvarchar(45)",
            "isTrialTest":false,
            "orderBy":2.0
         },
         {
            "id":189132,
            "input":"SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS\r\nWHERE TABLE_SCHEMA = DATABASE()\r\nORDER BY COLUMN_TYPE;",
            "output":"bigint\r\nchar(1)\r\ndate\r\ndatetime\r\ndecimal(10,2)\r\ndecimal(10,2)\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nint\r\nvarchar(10)\r\nvarchar(10)\r\nvarchar(20)\r\nvarchar(20)\r\nvarchar(45)\r\nvarchar(45)\r\nvarchar(45)\r\nvarchar(45)",
            "isTrialTest":false,
            "orderBy":2.0
         }
      ]
   }
}' http://localhost:8003/executeSubmission
done