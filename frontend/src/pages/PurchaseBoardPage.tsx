import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { boardService, gameService, transactionService } from '../services/api';
import { Game, BalanceResponse } from '../types';
import './PurchaseBoardPage.css';

const PurchaseBoardPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [games, setGames] = useState<Game[]>([]);
  const [balance, setBalance] = useState<number | null>(null);
  const [selectedGame, setSelectedGame] = useState<string>('');
  const [fieldCount, setFieldCount] = useState<number>(5);
  const [selectedNumbers, setSelectedNumbers] = useState<number[]>([]);
  const [loading, setLoading] = useState(true);
  const [purchasing, setPurchasing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const fieldCounts = [5, 6, 7, 8];
  const allNumbers = Array.from({ length: 16 }, (_, i) => i + 1);

  useEffect(() => {
    if (!user?.playerId) {
      navigate('/dashboard');
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);

        const [gamesRes, , balanceRes] = await Promise.all([
          gameService.getAllGames(),
          boardService.getPricing(),
          transactionService.getBalance(user.playerId!),
        ]);

        const activeGames = gamesRes.data.filter((g: Game) => !g.isClosed);
        setGames(activeGames);
        setBalance((balanceRes.data as BalanceResponse).balance);

        if (activeGames.length > 0) {
          setSelectedGame(activeGames[0].id);
        }
      } catch (err: any) {
        console.error('Error fetching data:', err);
        setError('Failed to load games. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user, navigate]);

  const handleNumberClick = (num: number) => {
    setSelectedNumbers((prev) => {
      if (prev.includes(num)) {
        return prev.filter((n) => n !== num);
      } else if (prev.length < fieldCount) {
        return [...prev, num];
      }
      return prev;
    });
  };

  const handleFieldCountChange = (count: number) => {
    setFieldCount(count);
    setSelectedNumbers([]);
    setError(null);
  };

  const getPriceForFieldCount = (): number => {
    const priceMap: { [key: number]: number } = {
      5: 20,
      6: 40,
      7: 80,
      8: 160,
    };
    return priceMap[fieldCount] || 0;
  };

  const handlePurchase = async () => {
    setError(null);

    // Validate game selection
    if (!selectedGame) {
      setError('Game selection required: Please select a game from the dropdown');
      return;
    }

    // Validate number count
    if (selectedNumbers.length === 0) {
      setError(`Number selection required: Please select ${fieldCount} numbers (1-16)`);
      return;
    }

    if (selectedNumbers.length !== fieldCount) {
      setError(`Invalid number count: You've selected ${selectedNumbers.length} numbers but need ${fieldCount}`);
      return;
    }

    // Check for duplicates (should not happen with UI, but validate anyway)
    const uniqueNumbers = new Set(selectedNumbers);
    if (uniqueNumbers.size !== selectedNumbers.length) {
      setError('Duplicate numbers detected: Please select each number only once');
      return;
    }

    // Validate all numbers are in range
    if (selectedNumbers.some((num) => num < 1 || num > 16)) {
      setError('Invalid numbers: All numbers must be between 1 and 16');
      return;
    }

    const price = getPriceForFieldCount();

    // Validate balance
    if (balance === null) {
      setError('Balance verification pending: Please try again');
      return;
    }

    if (balance < price) {
      const shortfall = price - balance;
      setError(
        `Insufficient balance: You need ${price} DKK but have ${balance} DKK (short by ${shortfall} DKK)`
      );
      return;
    }

    try {
      setPurchasing(true);
      setError(null);
      setSuccess(null);

      await boardService.purchaseBoard(user!.playerId!, selectedGame, fieldCount, selectedNumbers);

      setSuccess(`✓ Board purchased successfully for ${price} DKK!`);
      setTimeout(() => {
        navigate('/dashboard');
      }, 2000);
    } catch (err: any) {
      const errorMessage = err.response?.data?.error || 'Failed to purchase board. Please try again.';
      setError(`Purchase failed: ${errorMessage}`);
    } finally {
      setPurchasing(false);
    }
  };

  if (loading) {
    return <div className="loading-container">Loading...</div>;
  }

  if (!user?.playerId) {
    return <div className="loading-container">Redirecting...</div>;
  }

  return (
    <div className="purchase-container">
      <div className="purchase-header">
        <button onClick={() => navigate('/dashboard')} className="back-btn">
          ← Back to Dashboard
        </button>
        <h1>Purchase a Board</h1>
      </div>

      <div className="purchase-content">
        <div className="purchase-form">
          {error && <div className="error-message">{error}</div>}
          {success && <div className="success-message">{success}</div>}

          {/* Game Selection */}
          <div className="form-section">
            <h3>Select a Game</h3>
            {games.length === 0 ? (
              <div className="no-games">
                <p>No active games available at the moment.</p>
                <p style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
                  Admin must create a new game before you can purchase boards.
                </p>
              </div>
            ) : (
              <select
                value={selectedGame}
                onChange={(e) => setSelectedGame(e.target.value)}
                className="form-select"
              >
                <option value="">Choose a game...</option>
                {games.map((game) => (
                  <option key={game.id} value={game.id}>
                    Week of {new Date(game.weekStart).toLocaleDateString()} - Draw:{' '}
                    {new Date(game.drawTime).toLocaleString()}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Field Count Selection */}
          <div className="form-section">
            <h3>Select Number of Fields</h3>
            <div className="field-buttons">
              {fieldCounts.map((count) => (
                <button
                  key={count}
                  onClick={() => handleFieldCountChange(count)}
                  className={`field-btn ${fieldCount === count ? 'active' : ''}`}
                >
                  {count} Fields
                  <br />
                  <span className="field-price">{getPriceForFieldCount()} DKK</span>
                </button>
              ))}
            </div>
          </div>

          {/* Number Selection */}
          <div className="form-section">
            <h3>Select {fieldCount} Numbers from 1-16</h3>
            <div className="numbers-grid">
              {allNumbers.map((num) => (
                <button
                  key={num}
                  onClick={() => handleNumberClick(num)}
                  className={`number-btn ${
                    selectedNumbers.includes(num) ? 'selected' : ''
                  } ${selectedNumbers.length === fieldCount && !selectedNumbers.includes(num) ? 'disabled' : ''}`}
                  disabled={selectedNumbers.length === fieldCount && !selectedNumbers.includes(num)}
                >
                  {num}
                </button>
              ))}
            </div>
            <p className="selection-info">
              Selected: {selectedNumbers.length}/{fieldCount} - Numbers:{' '}
              {selectedNumbers.sort((a, b) => a - b).join(', ') || 'None'}
            </p>
          </div>

          {/* Summary */}
          <div className="form-section summary">
            <h3>Summary</h3>
            <p>
              <strong>Field Count:</strong> {fieldCount}
            </p>
            <p>
              <strong>Price:</strong> {getPriceForFieldCount()} DKK
            </p>
            <p>
              <strong>Current Balance:</strong> {balance !== null ? `${balance} DKK` : 'Loading...'}
            </p>
          </div>

          {/* Purchase Button */}
          <button
            onClick={handlePurchase}
            disabled={
              selectedNumbers.length !== fieldCount ||
              purchasing ||
              !selectedGame ||
              balance === null ||
              balance < getPriceForFieldCount()
            }
            className="purchase-btn"
          >
            {purchasing ? 'Purchasing...' : `Purchase Board (${getPriceForFieldCount()} DKK)`}
          </button>
        </div>
      </div>
    </div>
  );
};

export default PurchaseBoardPage;
