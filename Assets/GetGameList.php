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
$sql = "SELECT * FROM `ActiveGames` WHERE Active = 1";
$result = $conn->query($sql);

if ($result->num_rows > 0) {
    // Output data of each row
    while($row = $result->fetch_assoc()) {
        echo $row["Game ID"] . ":" . 
             ($row["TestGame"] ? 'true' : 'false') . ":" . 
             ($row["Active"] ? 'true' : 'false') . ":" . 
             $row["Map"] . ":" . 
             $row["MapWidth"] . ":" . 
             $row["MapHeight"] . "\n";
    }
} else {
    echo "0 results";
}
$conn->close();
?>
