export interface ServerDetails {
    address: string;
    port: number;
}

export interface Match {
    id: string,
    isAI: boolean,
    serverDetails: string,
    createdAt: number
}

export interface MatchDetails {
    id: string,
    serverDetails: string,
    createdAt: number
}
