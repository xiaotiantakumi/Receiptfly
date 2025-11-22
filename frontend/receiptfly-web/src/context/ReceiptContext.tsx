import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { type Receipt } from '../data/mockData';

// Assuming TransactionItem is defined elsewhere or needs to be defined.
// For the purpose of this edit, we'll assume it's a valid type.
// If not, you might need to define it or import it.
// Example: type TransactionItem = { id: number; isTaxReturn: boolean; category: string; aiCategory: string; aiRisk: string; memo: string; taxType: string; accountTitle: string; /* other properties */ };

interface TransactionItem {
  id: number;
  isTaxReturn: boolean;
  category: string;
  aiCategory: string;
  aiRisk: string;
  memo: string;
  taxType: string;
  accountTitle: string;
  // Add any other properties that a TransactionItem might have
}

interface ReceiptContextType {
  receipts: Receipt[];
  loading: boolean;
  updateItem: (receiptId: number, itemId: number, updates: Partial<TransactionItem>) => Promise<void>;
  updateReceipt: (receiptId: number, updates: Partial<Receipt>) => Promise<void>;
  createReceipt: (receipt: any) => Promise<Receipt | null>;
}

const ReceiptContext = createContext<ReceiptContextType | undefined>(undefined);

export function ReceiptProvider({ children }: { children: ReactNode }) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const fetchReceipts = async () => {
      setLoading(true);
      try {
        const res = await fetch('http://localhost:5159/api/receipts');
        const data = await res.json();
        setReceipts(data);
      } catch (err) {
        console.error('Failed to fetch receipts:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchReceipts();
  }, []);

  const updateItem = async (receiptId: number, itemId: number, updates: Partial<TransactionItem>) => {
    try {
      // Optimistic update
      setReceipts(prevReceipts =>
        prevReceipts.map(r =>
          r.id === receiptId
            ? { ...r, items: r.items.map(i => i.id === itemId ? { ...i, ...updates } : i) }
            : r
        )
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

  const updateReceipt = async (receiptId: number, updates: Partial<Receipt>) => {
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
    <ReceiptContext.Provider value={{ receipts, loading, updateItem, updateReceipt, createReceipt }}>
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
