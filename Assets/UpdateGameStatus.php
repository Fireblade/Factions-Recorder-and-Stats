<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

// Load configuration
$config = require 'config.php';

$storedApiKey = $config['api_key'];
$servername = $config['db_host'];
$username = $config['db_user'];
$password = $config['db_pass'];
$dbname = $config['db_name'];

// Check if the API key is provided and valid
if (!isset($_SERVER['HTTP_API_KEY']) || $_SERVER['HTTP_API_KEY'] !== $storedApiKey) {
    http_response_code(403);
    echo "Unauthorized access.";
    exit;
}

// Get the raw POST data
$rawData = file_get_contents("php://input");
$data = json_decode($rawData, true);

// Check if JSON decoding was successful
if (json_last_error() !== JSON_ERROR_NONE) {
    http_response_code(400);
    echo "JSON decode error: " . json_last_error_msg();
    exit;
}

// Verify that all required fields are present
$requiredFields = ['gameID', 'active', 'winner'];
foreach ($requiredFields as $field) {
    if (!isset($data[$field])) {
        http_response_code(400);
        echo "Missing parameter: $field";
        exit;
    }
}

// Extract data
$gameID = intval($data['gameID']);
$active = filter_var($data['active'], FILTER_VALIDATE_BOOLEAN);
$winner = strtoupper($data['winner']);

// Validate winner value
$validWinners = ['NONE', 'RED', 'BLUE', 'GREEN', 'YELLOW'];
if (!in_array($winner, $validWinners)) {
    http_response_code(400);
    echo "Invalid winner value.";
    exit;
}

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    http_response_code(500);
    echo "Connection failed: " . $conn->connect_error;
    exit;
}

// Prepare and bind
$stmt = $conn->prepare("UPDATE ActiveGames SET Active = ?, Winner = ? WHERE GameID = ?");
$stmt->bind_param("isi", $active, $winner, $gameID);

// Execute the statement
if ($stmt->execute()) {
    echo "Game status updated successfully.";
} else {
    http_response_code(500);
    echo "Error updating game status: " . $stmt->error;
}

// Close connection
$stmt->close();
$conn->close();
?>
