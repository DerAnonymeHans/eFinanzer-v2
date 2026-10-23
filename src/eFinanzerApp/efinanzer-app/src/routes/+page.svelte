<script lang="ts">
  import * as Card from '#lib/components/ui/card/index.js';
  import {useUserAccess} from "#lib/stores/user-access.svelte.ts";
  import {Button} from "#lib/components/ui/button/index.js";
  import {resolve} from "$app/paths";

  const userAccess = useUserAccess()
  const hasAnyAccess = $derived<boolean>(
    userAccess.value !== undefined
    && (userAccess.value.administrationRoles.length > 0
      || userAccess.value.accountingUnits.some(accountingUnit => accountingUnit.accountingUnitRoles.length > 0))
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
        <Button href={resolve('/profil')}>
          Zum Berechtigungen beantragen
        </Button>
      </div>
    {/if}
  </Card.Content>
</Card.Root>
