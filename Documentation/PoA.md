# Catalyst Proof of Authority consensus mechanism

> Intended to be used in private blockchain setups, this consensus mechanism is a simplified version of the innovative consensus mechanism developed for the Catalyst network.

## Election of the authorities

In this consensus algorithm, all nodes are delta producers and have an equal chance to be chosen for the production of the next [delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md) (a.k.a ledger state update). A delta consists of a list of transactions broadcast across the network that is produced once every ledger cycle. Each node store transactions in their transaction pool (a.k.a mempool).

> As an approximation for this proof of concept, we are assuming that the set of nodes on the network is small and stable enough to be known at all time by all participants.

A set of hashes is maintained by all participants, where each hash in the set is mapped to the identifier of a delta producer.

As a new delta _d_ is produced and identified by its unique hash _h<sub>d</sub>_, this set is updated with a new set of distinct hashes. Each hash _h<sub>i,d</sub>_ is obtained by hashing _h<sub>d</sub>_ concatenated with the identifier _pid<sub>i</sub>_ of a delta producer (cf. [peer identifier](https://github.com/catalyst-network/protocol-blueprint/blob/master/PeerProtocol.md#peer-identifier)) present on the network at this point in time.

This new set of hashes is then sorted in ascending order and used to rank the delta producers in order of authority. For example, the node with the identifier _pid<sub>i</sub>_ mapped to the lowest hash _h<sub>i,d</sub>_ in the set can be considered the favorite candidate for the production of the next delta, while the one with the highest _h<sub>i,d</sub>_ would be the least preferred.

## Production, voting, and synchronisation phases 

Each cycle resulting in the production of a ledger state update, or delta, is divided into 3 distinct parts:

- A production phase, during which a subset of producers (those with greater authority, a.k.a _producers-ga<sub>d</sub>_) build the next delta _d'_.
- A voting phase, during which all producers examine the content of the deltas they received and score them in order of preference.
- A synchronisation phase during which the next delta is propagated across all nodes so that each node can update their local copy of the ledger.

### Production phase

1. Each _producer-ga<sub>i,d</sub>_ node on the network builds its own version of the new delta based on the content of their mempool at the beginning of the ledger cycle, noted _d'<sub>i,d</ssub>_. 
2. Each _producer-ga<sub>i,d</sub>_ then submits its own version of the new delta (_d'<sub>i,d</ssub>_) to the rest of the network.

### Voting phase 

1. Each candidate delta _d'<sub>i,d</ssub>_ is ranked by compatibility and obtains a score _compatibility<sub>i</sub>_: the further their delta content is from the content in the local mempool of a voting node, the lower the candidate score _compatibility<sub>i</sub>_ is. 
2. Each candidate delta _d'<sub>i,d</ssub>_ is ranked by its producer's authority (cf. election process) and obtains a score _authority<sub>d</sub>_: the delta produced by the preferred producer will score the highest, while the delta produced by the least preferred producer will score the lowest.
3. The 2 previous scores _compatibility<sub>i</sub>_ and _authority<sub>d</sub>_ are then added together in a weighted sum to decide the final ranking of each _producer-ga<sub>i,d</sub>_ and its associated candidate delta _d'<sub>i,d</ssub>_.
4. Each node broadcast the hash of its favorite candidate delta _d'<sub>i,d</ssub>_ and the identifier of the corresponding _producer-ga<sub>i,d</sub>_ to the network.

### Synchronisation phase

Each node collects the hash _h<sub>d'</ssub>_ of the favorite delta broadcast by its peers and locally deduce the correct next delta _d'_ to use to update the ledger state. Each node can then request the next delta from the network.