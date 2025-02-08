<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

// Load configuration
$config = require 'config.php';

$servername = $config['servername'];
$username = $config['username'];
$password = $config['password'];
$dbname = $config['dbname'];

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

// Query to select active games
$sql = "SELECT `GameID`, `TestGame`, `Active`, `Map`, `MapWidth`, `MapHeight`, `VictoryGoal`, `waitingToStart`, `HQ_Iron_Multi`, `HQ_Wood_Multi`, `HQ_Worker_Multi`, `Building_Iron_Multi`, `Building_Wood_Multi`, `Building_Worker_Multi`, `GameMinute`, `AverageSoldierBlocks`, `AverageWorkerBlocks` FROM `ActiveGames` WHERE Active = 1 OR waitingToStart = 1";
$result = $conn->query($sql);

if ($result->num_rows > 0) {
    // Output data of each row
    while($row = $result->fetch_assoc()) {
        echo $row["GameID"] . ":" . 
             ($row["TestGame"] ? 'true' : 'false') . ":" . 
             ($row["Active"] ? 'true' : 'false') . ":" . 
             $row["Map"] . ":" . 
             $row["MapWidth"] . ":" . 
             $row["MapHeight"] . ":" . 
             $row["VictoryGoal"] . ":" . 
             ($row["waitingToStart"] ? 'true' : 'false') . ":" . 
             $row["HQ_Iron_Multi"] . ":" . 
             $row["HQ_Wood_Multi"] . ":" . 
             $row["HQ_Worker_Multi"] . ":" . 
             $row["Building_Iron_Multi"] . ":" . 
             $row["Building_Wood_Multi"] . ":" . 
             $row["Building_Worker_Multi"] . ":" . 
             $row["GameMinute"] . ":" . 
             $row["AverageSoldierBlocks"] . ":" . 
             $row["AverageWorkerBlocks"] . "\n";
    }
} else {
    echo "0 results";
}
$conn->close();
?>
