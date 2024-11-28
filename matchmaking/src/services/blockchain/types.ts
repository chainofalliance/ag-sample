export type Participant = {
    address: string,
    pubkey: Buffer,
    role: ParticipantRole
};

export type MatchData = {
    Id: string;
};

export type ActiveNode = {
    address: Buffer;
    name: string;
    url: string;
}

export enum ParticipantRole {
    PLAYER,
    MAIN,
    OBSERVER
}

export type Version = {
    version: string;
    version_hash: string;
}

export type DappInfo = {
    uid: string,
    version: string
}