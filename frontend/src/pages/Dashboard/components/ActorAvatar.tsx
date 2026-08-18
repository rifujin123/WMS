import { Avatar, Tooltip } from 'antd'
import { DEFAULT_AVATAR_URL } from '../../../lib/avatar'

interface ActorAvatarProps {
  name?: string
  avatarUrl?: string
  size?: number
}

/** Avatar người thao tác — hover hiện tên đầy đủ (ticket 21). */
function ActorAvatar({ name, avatarUrl, size = 24 }: ActorAvatarProps) {
  return (
    <Tooltip title={name || '—'}>
      <Avatar
        src={avatarUrl || DEFAULT_AVATAR_URL}
        size={size}
        style={{ backgroundColor: '#1677FF', cursor: 'pointer' }}
      />
    </Tooltip>
  )
}

export default ActorAvatar
