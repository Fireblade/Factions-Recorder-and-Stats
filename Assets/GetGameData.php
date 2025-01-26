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

if (isset($_POST['gameID'])) {
    $gameID = intval($_POST['gameID']);
} else {
    echo json_encode(["error" => "Missing parameters"]);
    exit;
}

// Check connection
if ($conn->connect_error) {
    die(json_encode(["error" => "Connection failed: " . $conn->connect_error]));
}

// Query to select game data with matching GameID
$sql = "SELECT GameMinute, AverageSoldierBlocks, AverageWorkerBlocks FROM `ActiveGames` WHERE GameID = ? AND Active = 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $gameID);
$stmt->execute();
$result = $stmt->get_result();

if ($result->num_rows > 0) {
    $data = $result->fetch_assoc();
    echo json_encode($data);
} else {
    echo "0 results";
}

$stmt->close();
$conn->close();
?>
