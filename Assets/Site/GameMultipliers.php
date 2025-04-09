<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");
header("Content-Type: application/json");

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
    die(json_encode(["error" => "Connection failed: " . $conn->connect_error]));
}

$selectedGameNumber = isset($_GET['gameID']) ? $_GET['gameID'] : -1;

if ($selectedGameNumber != -1) {
    $sql = "SELECT * FROM ActiveGames WHERE GameID = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("i", $selectedGameNumber);
} else {
    $sql = "SELECT * FROM ActiveGames ORDER BY GameID DESC LIMIT 1";
    $stmt = $conn->prepare($sql);
}

$stmt->execute();
$result = $stmt->get_result();

if ($result->num_rows > 0) {
    $row = $result->fetch_assoc();
    $response = [
        "HQ_Wood_Multi" => (float)$row['HQ_Wood_Multi'],
        "HQ_Iron_Multi" => (float)$row['HQ_Iron_Multi'],
        "HQ_Worker_Multi" => (float)$row['HQ_Worker_Multi'],
        "Building_Wood_Multi" => (float)$row['Building_Wood_Multi'],
        "Building_Iron_Multi" => (float)$row['Building_Iron_Multi'],
        "Building_Worker_Multi" => (float)$row['Building_Worker_Multi']
    ];

    echo json_encode($response);
} else {
    echo json_encode(["error" => "Game data not found."]);
}

$stmt->close();
$conn->close();
?>
