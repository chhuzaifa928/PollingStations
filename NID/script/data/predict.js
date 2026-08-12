async function tfjsLinearRegression(historicalData) {
    const years = historicalData.map(d => Number(d.year));
    const values = historicalData.map(d => Number(d.value));

    // Normalize input and output
    const minYear = Math.min(...years);
    const maxYear = Math.max(...years);
    const minValue = Math.min(...values);
    const maxValue = Math.max(...values);

    const normYears = years.map(y => (y - minYear) / (maxYear - minYear));
    const normValues = values.map(v => (v - minValue) / (maxValue - minValue));

    const inputTensor = tf.tensor2d(normYears, [normYears.length, 1]);
    const labelTensor = tf.tensor2d(normValues, [normValues.length, 1]);

    const model = tf.sequential();
    model.add(tf.layers.dense({ units: 1, inputShape: [1] }));
    model.compile({
        optimizer: tf.train.sgd(0.1), // Increased learning rate for small data
        loss: 'meanSquaredError'
    });

    await model.fit(inputTensor, labelTensor, {
        epochs: 500,
        verbose: 0
    });

    inputTensor.dispose();
    labelTensor.dispose();

    // Return prediction function (de-normalizing output)
    return function (year) {
        const normYear = (year - minYear) / (maxYear - minYear);
        const input = tf.tensor2d([[normYear]]);
        const output = model.predict(input);
        const normValue = output.dataSync()[0];
        input.dispose();
        output.dispose();

        if (isNaN(normValue)) return 0;

        // De-normalize the value back
        const denormValue = normValue * (maxValue - minValue) + minValue;
        console.log(`Predicted (normalized): ${normValue}, Final: ${denormValue}`);

        return denormValue;
    };
}
async function tfjsPolynomialRegression(historicalData) {
    const years = historicalData.map(d => Number(d.year));
    const values = historicalData.map(d => Number(d.value));

    // Normalize input and output
    const minYear = Math.min(...years);
    const maxYear = Math.max(...years);
    const minValue = Math.min(...values);
    const maxValue = Math.max(...values);

    const normYears = years.map(y => (y - minYear) / (maxYear - minYear));
    const normValues = values.map(v => (v - minValue) / (maxValue - minValue));

    // Expand features: [x, x^2]
    const inputTensor = tf.tensor2d(normYears.map(x => [x, x * x]));
    const labelTensor = tf.tensor2d(normValues, [normValues.length, 1]);

    const model = tf.sequential();
    model.add(tf.layers.dense({ units: 8, inputShape: [2], activation: 'relu' }));
    model.add(tf.layers.dense({ units: 1 }));
    model.compile({
        optimizer: tf.train.adam(0.01),
        loss: 'meanSquaredError'
    });

    await model.fit(inputTensor, labelTensor, {
        epochs: 500,
        verbose: 0
    });

    inputTensor.dispose();
    labelTensor.dispose();

    // Prediction function using same normalization
    return function (year) {
        const x = (year - minYear) / (maxYear - minYear);
        const input = tf.tensor2d([[x, x * x]]);
        const output = model.predict(input);
        const normValue = output.dataSync()[0];
        input.dispose();
        output.dispose();

        return normValue * (maxValue - minValue) + minValue;
    };
}

function linearRegression(data) {
    
    const n = data.length;
    const sumX = data.reduce((sum, d) => sum + d.year, 0);
    const sumY = data.reduce((sum, d) => sum + d.value, 0);
    const sumXY = data.reduce((sum, d) => sum + d.year * d.value, 0);
    const sumX2 = data.reduce((sum, d) => sum + d.year * d.year, 0);

    const denominator = n * sumX2 - sumX * sumX;
    if (denominator === 0) throw new Error("Linear regression error: division by zero");

    const slope = (n * sumXY - sumX * sumY) / denominator;
    const intercept = (sumY - slope * sumX) / n;

    return x => slope * x + intercept;
}

function movingAverageRegression(data, windowSize = 5) {
    // Clone and sort the data
    const historical = data.slice().sort((a, b) => a.year - b.year);

    return function (targetYear) {
        const all = historical.slice(); // will include future values step by step
        const maxYear = Math.max(...historical.map(d => d.year));

        // Predict from (maxYear + 1) to targetYear, step by step
        for (let year = maxYear + 1; year <= targetYear; year++) {
            const recent = all.slice(-windowSize);
            const avg = recent.reduce((sum, d) => sum + d.value, 0) / recent.length;
            all.push({ year, value: avg });
        }

        // Return the predicted value for targetYear
        return all.find(d => d.year === targetYear).value;
    };
}



function knnRegression(data, testYear, k) {
    function distance(a, b) {
        return Math.abs(a - b);
    }

    const distances = data.map(d => ({
        year: d.year,
        value: d.value,
        // Modified distance to emphasize newer years
        dist: Math.abs(d.year - testYear) * (1 + (testYear - d.year) * 0.05)
    }));

    distances.sort((a, b) => a.dist - b.dist);

    const neighbors = distances.slice(0, k);

    // Optional: Use weighted average based on distance
    let weightedSum = 0;
    let totalWeight = 0;

    neighbors.forEach(n => {
        const weight = 1 / (n.dist + 1e-6); // avoid division by zero
        weightedSum += n.value * weight;
        totalWeight += weight;
    });

    return weightedSum / totalWeight;
}



function exponentialRegression(data) {
    const n = data.length;
    const sumX = data.reduce((sum, d) => sum + d.year, 0);
    const sumY = data.reduce((sum, d) => sum + Math.log(d.value), 0);
    const sumXY = data.reduce((sum, d) => sum + d.year * Math.log(d.value), 0);
    const sumX2 = data.reduce((sum, d) => sum + d.year * d.year, 0);

    const denominator = n * sumX2 - sumX * sumX;
    if (denominator === 0) throw new Error("Exponential regression error");

    const b = (n * sumXY - sumX * sumY) / denominator;
    const a = Math.exp((sumY - b * sumX) / n);

    return x => a * Math.exp(b * x);
}


function polynomialRegression(data, degree = 3) {
    const X = data.map(d => d.year);
    const Y = data.map(d => d.value);
    const n = data.length;

    const A = [];
    for (let i = 0; i <= degree; i++) {
        A[i] = [];
        for (let j = 0; j <= degree; j++) {
            A[i][j] = X.reduce((sum, x) => sum + Math.pow(x, i + j), 0);
        }
    }

    const B = [];
    for (let i = 0; i <= degree; i++) {
        B[i] = X.reduce((sum, x, k) => sum + Math.pow(x, i) * Y[k], 0);
    }

    // Solve Ax = B using Gaussian elimination
    const coeffs = gaussianElimination(A, B);

    return x => coeffs.reduce((sum, c, i) => sum + c * Math.pow(x, i), 0);
}

function gaussianElimination(A, B) {
    const n = A.length;
    for (let i = 0; i < n; i++) {
        let maxRow = i;
        for (let j = i + 1; j < n; j++) {
            if (Math.abs(A[j][i]) > Math.abs(A[maxRow][i])) maxRow = j;
        }
        [A[i], A[maxRow]] = [A[maxRow], A[i]];
        [B[i], B[maxRow]] = [B[maxRow], B[i]];

        for (let j = i + 1; j < n; j++) {
            const factor = A[j][i] / A[i][i];
            for (let k = i; k < n; k++) {
                A[j][k] -= factor * A[i][k];
            }
            B[j] -= factor * B[i];
        }
    }

    const x = Array(n).fill(0);
    for (let i = n - 1; i >= 0; i--) {
        x[i] = B[i] / A[i][i];
        for (let j = 0; j < i; j++) {
            B[j] -= A[j][i] * x[i];
        }
    }
    return x;
}
