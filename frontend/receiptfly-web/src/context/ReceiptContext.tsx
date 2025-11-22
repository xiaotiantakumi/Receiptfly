import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { type Receipt } from '../data/mockData';

// Assuming TransactionItem is defined elsewhere or needs to be defined.
// For the purpose of this edit, we'll assume it's a valid type.
// If not, you might need to define it or import it.
// Example: type TransactionItem = { id: number; isTaxReturn: boolean; category: string; aiCategory: string; aiRisk: string; memo: string; taxType: string; accountTitle: string; /* other properties */ };

interface TransactionItem {
  id: string;
  isTaxReturn: boolean;
  category: string;
  aiCategory: string;
  aiRisk: string;
  memo: string;
  taxType: string;
  accountTitle: string;
  amount: number;
  name: string;
  // Add any other properties that a TransactionItem might have
}

interface ReceiptContextType {
  receipts: Receipt[];
  loading: boolean;
  updateItem: (receiptId: string, itemId: string, updates: Partial<TransactionItem>) => Promise<void>;
  updateReceipt: (receiptId: string, updates: Partial<Receipt>) => Promise<void>;
  createReceipt: (receipt: any) => Promise<Receipt | null>;
  refreshReceipts: () => Promise<void>;
}

const ReceiptContext = createContext<ReceiptContextType | undefined>(undefined);

export function ReceiptProvider({ children }: { children: ReactNode }) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  const fetchReceipts = async () => {
    setLoading(true);
    try {
      const res = await fetch('http://localhost:5159/api/receipts');
      if (!res.ok) {
        throw new Error(`HTTP error! status: ${res.status}`);
      }
      const data = await res.json();
      console.log('Fetched receipts:', data);
      console.log('Receipts count:', data.length);
      setReceipts(data || []);
    } catch (err) {
      console.error('Failed to fetch receipts:', err);
      setReceipts([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReceipts();
  }, []);

  const updateItem = async (receiptId: string, itemId: string, updates: Partial<TransactionItem>) => {
    try {
      // Optimistic update
      setReceipts(prevReceipts =>
        prevReceipts.map(r => {
          if (r.id === receiptId) {
            const updatedItems = r.items.map(i => i.id === itemId ? { ...i, ...updates } : i);
            const newTotal = updatedItems.reduce((sum, item) => sum + item.amount, 0);
            return { ...r, items: updatedItems, total: newTotal };
          }
          return r;
        })
      );

      await fetch(`http://localhost:5159/api/receipts/${receiptId}/items/${itemId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updates),
      });
    } catch (err) {
      console.error('Failed to update item:', err);
      // Revert optimistic update if needed (omitted for brevity)
    }
  };

  const updateReceipt = async (receiptId: string, updates: Partial<Receipt>) => {
    try {
      setReceipts(prevReceipts =>
        prevReceipts.map(r =>
          r.id === receiptId ? { ...r, ...updates } : r
        )
      );

      await fetch(`http://localhost:5159/api/receipts/${receiptId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updates),
      });
    } catch (err) {
      console.error('Failed to update receipt:', err);
    }
  };

  const createReceipt = async (receipt: any): Promise<Receipt | null> => {
    try {
      const res = await fetch('http://localhost:5159/api/receipts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(receipt),
      });

      if (!res.ok) {
        throw new Error('Failed to create receipt');
      }

      const newReceipt = await res.json();
      setReceipts(prev => [newReceipt, ...prev]);
      return newReceipt;
    } catch (err) {
      console.error('Failed to create receipt:', err);
      return null;
    }
  };

  return (
    <ReceiptContext.Provider value={{ receipts, loading, updateItem, updateReceipt, createReceipt, refreshReceipts: fetchReceipts }}>
      {children}
    </ReceiptContext.Provider>
  );
}

export const useReceipts = () => {
  const context = useContext(ReceiptContext);
  if (context === undefined) {
    throw new Error('useReceipts must be used within a ReceiptProvider');
  }
  return context;
};
