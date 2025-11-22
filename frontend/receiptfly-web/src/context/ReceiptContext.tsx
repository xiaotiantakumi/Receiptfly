import { createContext, useContext, useState, type ReactNode } from 'react';
import { MOCK_RECEIPTS, type Receipt } from '../data/mockData';

interface ReceiptContextType {
  receipts: Receipt[];
  updateItem: (receiptId: number, itemId: number, updates: Partial<{ isTaxReturn: boolean }>) => void;
}

const ReceiptContext = createContext<ReceiptContextType | undefined>(undefined);

export function ReceiptProvider({ children }: { children: ReactNode }) {
  const [receipts, setReceipts] = useState<Receipt[]>(MOCK_RECEIPTS);

  const updateItem = (receiptId: number, itemId: number, updates: Partial<{ isTaxReturn: boolean }>) => {
    setReceipts(prevReceipts => 
      prevReceipts.map(receipt => {
        if (receipt.id !== receiptId) return receipt;
        
        return {
          ...receipt,
          items: receipt.items.map(item => 
            item.id === itemId ? { ...item, ...updates } : item
          )
        };
      })
    );
  };

  return (
    <ReceiptContext.Provider value={{ receipts, updateItem }}>
      {children}
    </ReceiptContext.Provider>
  );
}

export function useReceipts() {
  const context = useContext(ReceiptContext);
  if (context === undefined) {
    throw new Error('useReceipts must be used within a ReceiptProvider');
  }
  return context;
}
