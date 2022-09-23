# mysql-run-skeleton-run-queries-and-check-database-single-db
for i in {01..256}; do
curl --location --request POST 'http://localhost:8003/ExecuteSubmission' \
--header 'Content-Type: application/json' \
--data-raw '{
    "id": 23665305,
    "executionType": "tests-execution",
    "executionStrategy": "mysql-run-skeleton-run-queries-and-check-database-single-db",
    "code": "UPDATE `employees` \nSET \n    `salary` = `salary` + 1000\nWHERE\n    `id` IN (SELECT \n            `leader_id`\n        FROM\n            `teams`)\n        AND `age` < 40\n        AND `salary` < 5000;",
    "timeLimit": 5000,
    "memoryLimit": 16777216,
    "executionDetails": {
        "maxPoints": 10,
        "checkerType": "trim",
        "tests": [
            {
                "id": 215245,
                "input": "SELECT SUM(`salary`) FROM `employees`;",
                "output": "505343.74\r\n",
                "isTrialTest": true,
                "orderBy": 1.0
            },
            {
                "id": 215246,
                "input": "SELECT * FROM `employees`\r\nWHERE\r\n    `id` IN (SELECT \r\n            `leader_id`\r\n        FROM\r\n            `teams`)\r\n        AND `age` < 40\r\n        AND `salary` > 5000; ",
                "output": "3\r\nRoddie\r\nKeenor\r\n33\r\n8149.56\r\nDesigner\r\nL\r\n5\r\nCraig\r\nRouzet\r\n32\r\n5740.32\r\nDesigner\r\nL\r\n11\r\nSay\r\nKennally\r\n31\r\n6555.27\r\nSales Manager\r\nH\r\n14\r\nBaron\r\nSange\r\n20\r\n8866.37\r\nDeveloper\r\nH\r\n15\r\nThayne\r\nCleave\r\n19\r\n6339.87\r\nMarketing Specialist\r\nH\r\n31\r\nGodwin\r\nSeares\r\n35\r\n8148.57\r\nMarketing Specialist\r\nN\r\n34\r\nYul\r\nBoulger\r\n31\r\n6338.81\r\nDeveloper\r\nL\r\n41\r\nHoracio\r\nBullough\r\n30\r\n5104.59\r\nDesigner\r\nH\r\n42\r\nHilton\r\nProsek\r\n27\r\n8019.54\r\nQuality Assurance\r\nN\r\n43\r\nCorney\r\nBradberry\r\n27\r\n5158.55\r\nDesigner\r\nL\r\n45\r\nHarris\r\nCoger\r\n36\r\n8006.17\r\nMarketing Specialist\r\nH\r\n47\r\nHomerus\r\nSpurway\r\n32\r\n5939.19\r\nMarketing Specialist\r\nN\r\n58\r\nZelig\r\nMacCaughen\r\n33\r\n8218.11\r\nMarketing Specialist\r\nH\r\n62\r\nHanson\r\nGoricke\r\n27\r\n5950.60\r\nDeveloper\r\nL\r\n63\r\nJanos\r\nMiddlewick\r\n27\r\n5743.85\r\nDeveloper\r\nH\r\n65\r\nMannie\r\nCaizley\r\n19\r\n5027.70\r\nDesigner\r\nH\r\n66\r\nReilly\r\nCoady\r\n22\r\n5420.82\r\nDeveloper\r\nN\r\n85\r\nJarrod\r\nManilo\r\n19\r\n7157.66\r\nSales Manager\r\nL\r\n88\r\nIsidro\r\nenzley\r\n33\r\n8784.27\r\nSales Manager\r\nH\r\n90\r\nBartolemo\r\nFalla\r\n21\r\n6535.56\r\nQuality Assurance\r\nL\r\n91\r\nArchambault\r\nRobak\r\n34\r\n5091.71\r\nMarketing Specialist\r\nL\r\n99\r\nDick\r\nBard\r\n20\r\n5193.83\r\nSales Manager\r\nN",
                "isTrialTest": false,
                "orderBy": 1.0
            }
        ],
        "TaskSkeleton": "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"
    }
}'
done