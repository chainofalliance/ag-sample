export type Participant = {
    address: string,
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
