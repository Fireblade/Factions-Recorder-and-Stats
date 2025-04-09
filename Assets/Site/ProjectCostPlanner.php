<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

$baseCost = isset($_GET['BaseCost']) ? $_GET['BaseCost'] : 1500;
$order = isset($_GET['order']) ? $_GET['order'] : '';
$startingMultiplier = isset($_GET['multiplier']) ? $_GET['multiplier'] : 1.0;

$smallNode = 1.15;
$bigNode = 1.52;

$multiplier = 1;
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Project Cost Planner</title>
    <style>
        body {
            background-color: #8E6B43;
            color: white;
            font-family: Arial, sans-serif;
        }
        table, th, td {
            border: 1px solid white;
            border-collapse: collapse;
            padding: 5px;
        }
        th, td {
            text-align: center;
        }
        button {
            background-color: white;
            color: #8A3F00;
            border: none;
            padding: 10px 20px;
            margin: 5px;
            cursor: pointer;
        }
        button:hover {
            background-color: #6E2F00;
            color: white;
        }
        textarea {
            width: 100%;
            height: 50px;
        }
        input[type="number"] {
            padding: 5px;
            margin: 5px;
            width: 100px;
        }
    </style>
    <script>
        let baseCost = <?php echo $baseCost; ?>;
        let smallNodeMultiplier = <?php echo $smallNode; ?>;
        let bigNodeMultiplier = <?php echo $bigNode; ?>;
        let maxMultiplier = 30;
        let nodes = [];
        let startingMultiplier = <?php echo $startingMultiplier; ?>;
        let order = '<?php echo $order; ?>';

        function addNode(type) {
            let multiplier = type === 'small' ? smallNodeMultiplier : bigNodeMultiplier;
            let cost = type === 'small' ? baseCost : baseCost * 3;
            let node = { type, cost, multiplier };
            nodes.push(node);
            updateTable();
            updateURL();
        }

        function removeNode(index) {
            nodes.splice(index, 1);
            updateTable();
            updateURL();
        }

        function updateTable() {
            let table = document.getElementById('plannerTable');
            table.innerHTML = '';

            let typeRow = table.insertRow();
            let descriptionRow = table.insertRow();
            let costRow = table.insertRow();
            let totalCostRow = table.insertRow();
            let multiplierRow = table.insertRow();
            let actionRow = table.insertRow();

            typeRow.insertCell().innerText = 'Type';
            descriptionRow.insertCell().innerHTML = `Description`;
            costRow.insertCell().innerText = 'Node Cost';
            totalCostRow.insertCell().innerText = 'Total Cost';
            multiplierRow.insertCell().innerText = 'Multiplier';
            actionRow.insertCell().innerHTML = `Remove?`;

            let currentMultiplier = startingMultiplier;
            let totalCost = 0;

            nodes.forEach((node, index) => {

                let cost = node.cost * (index > 0 ? currentMultiplier : startingMultiplier);
                totalCost += cost;

                let cellMulti = currentMultiplier;
                currentMultiplier *= node.multiplier;
                if (currentMultiplier > maxMultiplier) currentMultiplier = maxMultiplier;

                typeRow.insertCell().innerText = node.type.charAt(0).toUpperCase() + node.type.slice(1);
                descriptionRow.insertCell().innerHTML = `<textarea style="height: 26px; font-size: 16px; background-color: rgba(142, 107, 67, 0.6); color: white; text-align: center;"></textarea>`;
                costRow.insertCell().innerText = cost.toFixed(2);
                totalCostRow.insertCell().innerText = totalCost.toFixed(2);
                multiplierRow.insertCell().innerText = cellMulti.toFixed(2);
                actionRow.insertCell().innerHTML = `<button onclick="removeNode(${index})">Remove</button>`;
            });
        }

        function updateBaseCost() {
            let newBaseCost = document.getElementById('baseCostInput').value;
            if (newBaseCost) {
                baseCost = parseFloat(newBaseCost);
                nodes.forEach(node => {
                    node.cost = node.type === 'small' ? baseCost : baseCost * 3;
                });
                updateTable();
                updateURL();
            }
        }

        function updateStartingMultiplier() {
            let newStartingMultiplier = document.getElementById('startingMultiplierInput').value;
            if (newStartingMultiplier) {
                startingMultiplier = parseFloat(newStartingMultiplier);
                updateTable();
                updateURL();
            }
        }

        function initializeNodes() {
            for (let char of order) {
                if (char === 's') {
                    addNode('small');
                } else if (char === 'b') {
                    addNode('big');
                }
            }
        }

        function updateURL() {
            let orderString = nodes.map(node => node.type.charAt(0)).join('');
            let newURL = `${window.location.pathname}?BaseCost=${baseCost}&order=${orderString}`;
            if (startingMultiplier !== 1.0) {
                newURL += `&multiplier=${startingMultiplier}`;
            }
            window.history.replaceState(null, '', newURL);
        }

        window.onload = function() {
            initializeNodes();
        };
    </script>
</head>
<body>
    <h1>Project Cost Planner</h1>
    <button onclick="addNode('small')">Add Small Node</button>
    <button onclick="addNode('big')">Add Big Node</button>
    <button onclick="removeNode(nodes.length - 1)">Remove Last Node</button>
    <label for="baseCostInput">Base Cost:</label>
    <input type="number" id="baseCostInput" value="<?php echo $baseCost; ?>" onchange="updateBaseCost()">
    <label for="startingMultiplierInput">Starting Multiplier:</label>
    <input type="number" step="0.01" id="startingMultiplierInput" value="<?php echo $startingMultiplier; ?>" onchange="updateStartingMultiplier()">
    <table id="plannerTable">
        <!-- Table content will be dynamically generated -->
    </table>
</body>
</html>


