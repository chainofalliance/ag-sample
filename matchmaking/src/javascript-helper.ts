import * as crypto from 'crypto';

export function now(): number {
    return Math.floor(nowMS() / 1000);
}

export function nowMS(): number {
    return Date.now();
}

export function guid(): string {
    return crypto.randomUUID();
}

export function unreachable(unreachable: never): never {
    throw new Error(
        'Should have been unreachable but looks like it wasnt: '
        + JSON.stringify(unreachable),
    );
}

export async function sleep(milliseconds: number): Promise<void> {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

export async function retry<T>(
    fn: () => Promise<T>,
    attempts = 20,
    waitBetween = 1000,
): Promise<T> {
    let lastError: unknown;

    for (let i = 0; i < attempts; i++) {
        if (i > 0) {
            // eslint-disable-next-line no-await-in-loop
            await sleep(waitBetween);
        }

        try {
            // eslint-disable-next-line no-await-in-loop
            const result = await fn();
            if (result) {
                return result;
            }
        } catch (error: unknown) {
            lastError = error;
        }
    }

    throw lastError;
}

export function typedKeys<T extends keyof any>(
    record: Readonly<Partial<Record<T, unknown>>>,
): T[] {
    if (!record) {
        return [];
    }

    return (Object.keys(record) as unknown[]) as T[];
}

export function typedEntities<K extends keyof any, V>(
    record: Readonly<Partial<Record<K, V>>>,
): Array<[K, V]> {
    if (!record) {
        return [];
    }

    return (Object.entries(record) as unknown[]) as Array<[K, V]>;
}

export function sortBy<T>(
    selector: (input: T) => number,
    reverse = false,
): (a: T, b: T) => number {
    if (reverse) {
        return (a, b) => selector(b) - selector(a);
    }

    return (a, b) => selector(a) - selector(b);
}

export function arrayZip<A, B>(
    a: readonly A[],
    b: readonly B[],
): Array<[A, B]> {
    if (a.length !== b.length) {
        throw new Error('array length is different');
    }

    return a.map((a, i): [A, B] => [a, b[i]!]);
}
