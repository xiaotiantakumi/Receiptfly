import { v4 as uuidv4 } from 'uuid';

/**
 * レシートIDを生成
 * @returns receipt-{uuid} 形式のID
 */
export function generateReceiptId(): string {
  return `receipt-${uuidv4()}`;
}

/**
 * 取引明細項目IDを生成
 * @returns transaction-{uuid} 形式のID
 */
export function generateTransactionItemId(): string {
  return `transaction-${uuidv4()}`;
}

