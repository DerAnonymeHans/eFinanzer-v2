<script lang="ts">
  import * as Card from '#lib/components/ui/card/index.js';
  import {useUserAccess} from "#lib/stores/user-access.svelte.ts";
  import {Button} from "#lib/components/ui/button/index.js";

  const userAccessStore = useUserAccess()
  const hasAnyAccess = $derived<boolean>(
    userAccessStore.userAccess !== undefined
    && (userAccessStore.userAccess.administrationRoles.length > 0
      || userAccessStore.userAccess.accountingUnits.some(accountingUnit => accountingUnit.accountingUnitRoles.length > 0))
  )

</script>

<Card.Root>
  <Card.Header>
    <Card.Title>Willkommen beim eFinanzer!</Card.Title>

  </Card.Header>
  <Card.Content>
    {#if !hasAnyAccess}
      <Card.Description>Du besitzt zur Zeit keine Berechtigungen. Um Berechtigungen zu erhalten, klicke auf den Button und erstelle anschließend einen Rollenänderungsantrag.</Card.Description>
      <div>
        <Button>
          <a href="./profil">Zum Berechtigungen beantragen</a>
        </Button>
      </div>
    {/if}
  </Card.Content>
</Card.Root>
