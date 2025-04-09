<?php
header('Content-Type: application/json');

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

// Check if userID is set
if (isset($_GET['userID'])) {
    $userID = intval($_GET['userID']);
    $sql = "SELECT PreviousName FROM PlayerPreviousNames WHERE PlayerID = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("i", $userID);
    $stmt->execute();
    $result = $stmt->get_result();

    $previousNames = [];
    while ($row = $result->fetch_assoc()) {
        $previousNames[] = $row['PreviousName'];
    }

    echo json_encode($previousNames);
} else {
    echo json_encode([]);
}

// Close connection
$conn->close();
?>

